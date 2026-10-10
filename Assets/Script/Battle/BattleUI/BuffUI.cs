using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BuffUI : MonoBehaviour
{
    private enum BuffTarget
    {
        Player,
        Monster
    }

    private const int DefaultBuffIconId = 1000;

    [Header("버프 아이콘 설정")]
    // 계층의 나열 순서와 무관하게 실제 배치된 위치를 좌측 상단부터 행 순서로 사용
    // 이 순서로 이미지를 인스펙터에 직접 연결하며 런타임에는 좌표를 계산하지 않음
    public List<Image> iconSlots = new();
    [SerializeField] private BuffTarget target = BuffTarget.Player;
    [SerializeField] private Monster targetMonster;

    private readonly Dictionary<int, Sprite> iconCache = new();
    private string currentBuffSignature;

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
        ClearSlots();
        currentBuffSignature = null;
    }

    private void OnDestroy()
    {
        iconCache.Clear();
    }

    public void Refresh()
    {
        List<int> activeBuffIds = CollectActiveBuffIds();
        UpdateSlots(activeBuffIds);
        currentBuffSignature = BuildBuffSignature(activeBuffIds);
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
        List<int> activeBuffIds = CollectActiveBuffIds();
        string nextBuffSignature = BuildBuffSignature(activeBuffIds);
        if (currentBuffSignature == nextBuffSignature)
        {
            return;
        }

        UpdateSlots(activeBuffIds);
        currentBuffSignature = nextBuffSignature;
    }

    private List<int> CollectActiveBuffIds()
    {
        List<int> activeBuffIds = new();
        List<Buff> sourceBuffs = ResolveSourceBuffs();
        if (sourceBuffs == null)
        {
            return activeBuffIds;
        }

        foreach (Buff buff in sourceBuffs)
        {
            int buffId = GetBuffId(buff);
            if (buffId > 0)
            {
                activeBuffIds.Add(buffId);
            }
        }

        return activeBuffIds;
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
        if (buff == null || buff.stack <= 0 || buff.data == null)
        {
            return 0;
        }

        return buff.data.buffId;
    }

    private static string BuildBuffSignature(List<int> activeBuffIds)
    {
        if (activeBuffIds == null || activeBuffIds.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach (int buffId in activeBuffIds)
        {
            builder.Append(buffId);
            builder.Append('|');
        }

        return builder.ToString();
    }

    private void UpdateSlots(List<int> activeBuffIds)
    {
        ClearSlots();

        if (activeBuffIds == null)
        {
            return;
        }

        // 유효한 버프를 첫 슬롯부터 다시 채워 중간에 사라진 버프의 빈칸을 당김
        int visibleCount = Mathf.Min(activeBuffIds.Count, iconSlots.Count);
        for (int i = 0; i < visibleCount; i++)
        {
            SetIconSprite(activeBuffIds[i], iconSlots[i]);
        }
    }

    private void SetIconSprite(int buffId, Image iconImage)
    {
        if (iconImage == null || buffId <= 0)
        {
            return;
        }

        if (iconCache.TryGetValue(buffId, out Sprite cachedSprite) && cachedSprite != null)
        {
            ApplySprite(iconImage, cachedSprite);
            return;
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
            return;
        }

        iconCache[buffId] = loadedSprite;
        ApplySprite(iconImage, loadedSprite);
    }

    private string BuildIconResourcePath(int buffId)
    {
        return $"Image/BuffIcon/{buffId}";
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = true;
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;
    }

    private void ClearSlots()
    {
        foreach (Image image in iconSlots)
        {
            if (image != null)
            {
                image.enabled = false;
                image.sprite = null;
            }
        }
    }
}
