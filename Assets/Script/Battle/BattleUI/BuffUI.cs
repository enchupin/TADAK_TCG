using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class BuffUI : MonoBehaviour
{
    private enum BuffTarget
    {
        Player,
        Monster
    }

    private const int DefaultBuffIconId = 1000;
    // 족보(1028)는 더블·풀하우스·스트레이트·로열을 하나의 아이콘으로 묶어 표시 (R-79)
    private const int HandRankBuffId = 1028;
    private static readonly HashSet<int> HandRankMemberBuffIds = new() { 1007, 1009, 1010, 1024 };

    [Header("버프 아이콘 설정")]
    [SerializeField] private BuffTarget target = BuffTarget.Player;
    [SerializeField] private Monster targetMonster;

    [Header("아이콘 목록")]
    // 필요한 만큼 아이콘을 만들어 재사용하고, 남는 아이콘은 끄기만 함
    [Tooltip("버프 아이콘 프리팹 (StatusIconView), 비어 있으면 기본 아이콘을 만들어 사용")]
    [SerializeField] private StatusIconView iconPrefab;
    [Tooltip("아이콘을 놓을 영역, 비어 있으면 이 오브젝트. 좌측 상단부터 채우고 폭을 넘으면 다음 줄")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private float iconSize = 30f;
    [SerializeField] private float iconSpacing = 2f;
    [Tooltip("기본 아이콘의 중첩 숫자 글자 크기 (프리팹을 쓰면 프리팹 설정 사용)")]
    [SerializeField] private float stackFontSize = 16f;
    [Tooltip("연결하면 이 영역(체력 바)의 좌우 길이를 아이콘 영역 폭으로 사용")]
    [SerializeField] private RectTransform widthSource;

    [Header("툴팁")]
    // 슬롯에 TooltipTrigger가 없으면 실행 시 추가하고 이 간격으로 아이콘 하단에 표시
    [Tooltip("아이콘과 툴팁 사이 간격 (슬롯에 TooltipTrigger를 직접 붙인 경우 그 설정 사용)")]
    [SerializeField] private float tooltipSpacing = 4f;

    private readonly Dictionary<int, Sprite> iconCache = new();
    // 슬롯 순서대로 표시 중인 버프 ID (툴팁 조회용)
    private readonly List<int> displayedBuffIds = new();
    // 만들어 둔 아이콘, 앞에서부터 표시 중인 버프 수만큼 사용
    private readonly List<StatusIconView> iconPool = new();
    private string currentBuffSignature;
    private float laidOutWidth = -1f;

    private void OnEnable()
    {
        currentBuffSignature = null;
        RefreshIfNeeded();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshIfNeeded();
    }

    // Update is called once per frame
    void Update()
    {
        RefreshIfNeeded();
    }

    private void OnDisable()
    {
        // 비활성화 도중에는 자식 활성 상태를 바꾸지 않고 내용만 비움
        ClearSlots(false);
        currentBuffSignature = null;
    }

    private void OnDestroy()
    {
        iconCache.Clear();
    }

    public void Refresh()
    {
        List<BuffIconEntry> activeBuffs = CollectActiveBuffs();
        UpdateSlots(activeBuffs);
        currentBuffSignature = BuildBuffSignature(activeBuffs);
    }

    // 몬스터 버프 영역은 상태 컨트롤러가 체력 바를 지정 (인스펙터에 연결한 값이 우선)
    public void SetWidthSourceIfEmpty(RectTransform source)
    {
        if (widthSource == null)
        {
            widthSource = source;
        }
    }

    public void BindMonster(Monster monster)
    {
        if (target == BuffTarget.Monster && targetMonster == monster)
        {
            return;
        }

        ClearSlots();
        target = BuffTarget.Monster;
        targetMonster = monster;
        currentBuffSignature = null;
        RefreshIfNeeded();
    }

    public void UnbindMonster(Monster monster)
    {
        // 이전 몬스터가 제거되더라도 같은 스폰 칸에 새로 연결된 몬스터의 버프는 유지
        // 버프를 넘겨주는 것이 아니라 현재 표시 대상과 해제 요청의 몬스터가 같을 때만 슬롯을 비움
        if (target != BuffTarget.Monster || targetMonster != monster)
        {
            return;
        }

        targetMonster = null;
        ClearSlots();
        currentBuffSignature = null;
    }

    private void RefreshIfNeeded()
    {
        bool widthChanged = SyncContainerWidth();
        List<BuffIconEntry> activeBuffs = CollectActiveBuffs();
        string nextBuffSignature = BuildBuffSignature(activeBuffs);
        if (currentBuffSignature == nextBuffSignature)
        {
            if (widthChanged)
            {
                LayoutIcons();
            }
            return;
        }

        UpdateSlots(activeBuffs);
        currentBuffSignature = nextBuffSignature;
    }

    // 표시할 버프 하나 (중첩 숫자는 중첩 가능한 버프만)
    private readonly struct BuffIconEntry
    {
        public readonly int BuffId;
        public readonly int Stack;
        public readonly bool ShowStack;

        public BuffIconEntry(int buffId, int stack, bool showStack)
        {
            BuffId = buffId;
            Stack = stack;
            ShowStack = showStack;
        }
    }

    private List<BuffIconEntry> CollectActiveBuffs()
    {
        List<BuffIconEntry> activeBuffs = new();
        List<Buff> sourceBuffs = ResolveSourceBuffs();
        if (sourceBuffs == null)
        {
            return activeBuffs;
        }

        bool hasHandRank = false;
        foreach (Buff buff in sourceBuffs)
        {
            int buffId = GetBuffId(buff);
            if (buffId <= 0)
            {
                continue;
            }

            if (target == BuffTarget.Player && HandRankMemberBuffIds.Contains(buffId))
            {
                if (!hasHandRank)
                {
                    hasHandRank = true;
                    // 족보 묶음은 여러 버프를 합친 아이콘이라 숫자 없음
                    activeBuffs.Add(new BuffIconEntry(HandRankBuffId, 0, false));
                }
                continue;
            }

            // 툴팁의 {stack}과 같은 값(buff.stack)을 표시
            activeBuffs.Add(new BuffIconEntry(buffId, buff.stack, !BuffData.IsNonStackableBuffId(buffId)));
        }

        return activeBuffs;
    }

    private List<Buff> ResolveSourceBuffs()
    {
        if (target == BuffTarget.Monster)
        {
            return targetMonster != null && targetMonster.isActiveAndEnabled && !targetMonster.IsDead()
                ? targetMonster.currentBuffs
                : null;
        }

        PlayerData player = PlayerData.Instance;
        return player != null ? player.currentBuffs : null;
    }

    private static int GetBuffId(Buff buff)
    {
        // 힘처럼 음수 중첩을 가진 효과도 표시
        if (buff == null || buff.data == null)
        {
            return 0;
        }

        // 0 중첩은 숨기되, 0으로 보유를 유지하는 패시브(거울)는 표시
        if (buff.stack == 0 && !BuffData.IsPassiveBuffId(buff.data.buffId))
        {
            return 0;
        }

        return buff.data.buffId;
    }

    private static string BuildBuffSignature(List<BuffIconEntry> activeBuffs)
    {
        if (activeBuffs == null || activeBuffs.Count == 0)
        {
            return string.Empty;
        }

        // 중첩이 바뀌어도 숫자를 다시 그리도록 중첩까지 포함
        StringBuilder builder = new();
        foreach (BuffIconEntry entry in activeBuffs)
        {
            builder.Append(entry.BuffId);
            builder.Append(':');
            builder.Append(entry.ShowStack ? entry.Stack : 0);
            builder.Append('|');
        }

        return builder.ToString();
    }

    private void UpdateSlots(List<BuffIconEntry> activeBuffs)
    {
        // 호버 중인 아이콘이 꺼졌다 켜지면 툴팁이 닫히므로 쓰는 아이콘은 끄지 않고 내용만 바꿈
        displayedBuffIds.Clear();

        if (activeBuffs == null)
        {
            HideIconsFrom(0);
            return;
        }

        // 유효한 버프를 첫 슬롯부터 다시 채워 중간에 사라진 버프의 빈칸을 당김
        // 칸 수 제한 없이 부족한 아이콘은 새로 만듦
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            StatusIconView icon = GetOrCreateIcon(i);
            if (icon == null)
            {
                break;
            }

            BuffIconEntry entry = activeBuffs[i];
            if (!icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
            }

            icon.Set(LoadIconSprite(entry.BuffId), entry.ShowStack ? entry.Stack.ToString() : null);
            displayedBuffIds.Add(entry.BuffId);
        }

        HideIconsFrom(displayedBuffIds.Count);
        LayoutIcons();
    }

    private void HideIconsFrom(int startIndex, bool deactivate = true)
    {
        for (int i = Mathf.Max(0, startIndex); i < iconPool.Count; i++)
        {
            StatusIconView icon = iconPool[i];
            if (icon != null && icon.gameObject.activeSelf)
            {
                icon.Set(null, null);
                if (deactivate)
                {
                    icon.gameObject.SetActive(false);
                }
            }
        }
    }

    private RectTransform ResolveContainer()
    {
        return iconContainer != null ? iconContainer : transform as RectTransform;
    }

    // 아이콘마다 툴팁 트리거를 한 번만 연결하고 아이콘 번호로 현재 버프 문구를 조회
    private StatusIconView GetOrCreateIcon(int index)
    {
        while (iconPool.Count <= index)
        {
            RectTransform container = ResolveContainer();
            if (container == null)
            {
                return null;
            }

            StatusIconView icon = iconPrefab != null
                ? Instantiate(iconPrefab, container, false)
                : StatusIconView.CreateDefault(container, "BuffIcon", iconSize, stackFontSize);
            icon.name = $"BuffIcon ({iconPool.Count})";

            RectTransform rect = icon.RectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            icon.SetSize(iconSize);

            if (!icon.TryGetComponent(out TooltipTrigger trigger))
            {
                trigger = icon.gameObject.AddComponent<TooltipTrigger>();
                trigger.SetPlacement(TooltipPlacement.Bottom, tooltipSpacing);
            }

            trigger.SetSource(new BuffSlotTooltipSource(this, iconPool.Count));
            icon.gameObject.SetActive(false);
            iconPool.Add(icon);
        }

        return iconPool[index];
    }

    // 컨테이너 폭에 맞춰 좌측 상단부터 줄바꿈 배치
    private void LayoutIcons()
    {
        RectTransform container = ResolveContainer();
        if (container == null)
        {
            return;
        }

        int columns = IconRowLayout.ResolveColumns(container.rect.width, iconSize, iconSpacing);
        for (int i = 0; i < displayedBuffIds.Count && i < iconPool.Count; i++)
        {
            (float x, float y) = IconRowLayout.WrapCellTopLeft(i, columns, iconSize, iconSpacing);
            iconPool[i].RectTransform.anchoredPosition = new Vector2(x, y);
        }
    }

    // 폭 기준 영역(체력 바)과 아이콘 영역의 좌우 길이를 맞춤, 바뀌었으면 true
    private bool SyncContainerWidth()
    {
        RectTransform container = ResolveContainer();
        if (container == null)
        {
            return false;
        }

        if (widthSource != null)
        {
            float sourceWidth = widthSource.rect.width;
            if (sourceWidth > 0f && !Mathf.Approximately(container.rect.width, sourceWidth))
            {
                container.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sourceWidth);
            }
        }

        float width = container.rect.width;
        if (Mathf.Approximately(laidOutWidth, width))
        {
            return false;
        }

        laidOutWidth = width;
        return true;
    }

    private string GetSlotTooltipText(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= displayedBuffIds.Count)
        {
            return string.Empty;
        }

        int buffId = displayedBuffIds[slotIndex];
        List<Buff> sourceBuffs = ResolveSourceBuffs();
        if (sourceBuffs == null)
        {
            return string.Empty;
        }

        if (buffId == HandRankBuffId && target == BuffTarget.Player)
        {
            return BuffTooltipTextBuilder.BuildHandRank(sourceBuffs);
        }

        // 표시 중 중첩이 바뀌어도 현재 값을 읽도록 매번 목록에서 찾음
        foreach (Buff buff in sourceBuffs)
        {
            if (GetBuffId(buff) == buffId)
            {
                return BuffTooltipTextBuilder.Build(buff);
            }
        }

        return string.Empty;
    }

    private sealed class BuffSlotTooltipSource : ITooltipSource
    {
        private readonly BuffUI owner;
        private readonly int slotIndex;

        public BuffSlotTooltipSource(BuffUI owner, int slotIndex)
        {
            this.owner = owner;
            this.slotIndex = slotIndex;
        }

        public string GetTooltipText()
        {
            return owner != null ? owner.GetSlotTooltipText(slotIndex) : string.Empty;
        }
    }

    private Sprite LoadIconSprite(int buffId)
    {
        if (buffId <= 0)
        {
            return null;
        }

        if (iconCache.TryGetValue(buffId, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(BuildIconResourcePath(buffId));
        if (loadedSprite == null)
        {
            loadedSprite = Resources.Load<Sprite>(BuildIconResourcePath(DefaultBuffIconId));
            Debug.LogWarning($"버프 아이콘을 찾을 수 없습니다: {buffId}");
        }

        if (loadedSprite == null)
        {
            Debug.LogError($"버프 아이콘을 찾을 수 없습니다: {buffId}");
            return null;
        }

        iconCache[buffId] = loadedSprite;
        return loadedSprite;
    }

    private string BuildIconResourcePath(int buffId)
    {
        return $"Image/BuffIcon/{buffId}";
    }

    private void ClearSlots(bool deactivateIcons = true)
    {
        displayedBuffIds.Clear();
        HideIconsFrom(0, deactivateIcons);
    }
}
