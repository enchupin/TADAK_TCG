using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조우별 스폰 좌표 조정 씬 (Tools/Battle/스폰 배치 씬 만들기로 생성)
/// 1920×1080 캔버스, 우측 절반(몬스터 영역)만 배경색으로 구분
/// 조우를 고르면 실제 몬스터 프리팹을 칸 좌표에 Idle로 표시, 드래그 · 슬라이더로 x · y 조정
/// 저장하면 JsonData/spawnLayouts.json에 기록되어 전투에 그대로 반영
/// </summary>
public class SpawnLayoutPreview : MonoBehaviour
{
    private const int MaxSlots = 4;
    private const float ScreenWidth = 1920f;
    private const float RegionWidth = 960f;

    [Header("참조")]
    [Tooltip("전투에서 쓰는 몬스터 프리팹 (Monster(Temp))")]
    [SerializeField] private GameObject monsterPrefab;
    [Tooltip("전투 씬 MonsterSpawnPositions와 같은 기준 (캔버스 우측 중앙)")]
    [SerializeField] private RectTransform spawnRoot;

    [Header("스폰 위치 (전투 씬 MonsterSpawner (1)에서 측정)")]
    [SerializeField] private Vector2 spawnerAnchor = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 spawnerPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 spawnerSize = new Vector2(150f, 150f);
    [Tooltip("몬스터 스폰 위치 하단 기준 플레이어 파티 하단 높이")]
    [SerializeField] private float partyBottomFromSpawn;

    [Header("기준선")]
    [Tooltip("지면 기준 높이 눈금 간격")]
    [SerializeField] private float heightGridStep = 100f;
    [SerializeField] private int heightGridCount = 4;

    [Header("표시 색")]
    [SerializeField] private Color spawnLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color groundLineColor = new Color(0.3f, 1f, 0.3f, 0.8f);
    [SerializeField] private Color partyLineColor = new Color(1f, 0.6f, 0.2f, 0.8f);
    [SerializeField] private Color heightGridColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color contentBoxColor = new Color(1f, 0.2f, 0.2f, 0.2f);
    [SerializeField] private Color feetMarkColor = new Color(0.3f, 1f, 0.3f, 1f);
    [SerializeField] private Color summonTint = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private Color selectedLabelColor = new Color(1f, 0.9f, 0.3f, 1f);

    private class Slot
    {
        public RectTransform root;
        public MonsterStateController controller;
        public Image contentBox;
        public RectTransform feetMark;
        public Text label;
        public string imageName;
        public bool isSummon;
        // spawnRoot 기준 Idle 그림 좌우 끝 · 발 높이, 체력 바 좌우 끝
        public float contentLeft;
        public float contentRight;
        public float feet;
        public float barLeft;
        public float barRight;
    }

    private readonly List<Slot> slots = new();
    private readonly List<string> layoutIds = new();
    private readonly List<string> warnings = new();
    private SpawnLayoutData data;
    private Font labelFont;
    private int selectedLayout;
    private int selectedSlot;
    private bool dragHorizontalOnly = true;
    private bool showContentBox = true;
    private bool dirty;
    private Rect windowRect = new Rect(10f, 10f, 360f, 760f);
    private Vector2 listScroll;
    private string statusMessage = string.Empty;

    private SpawnLayoutEntry CurrentEntry => layoutIds.Count > 0 ? GetOrCreateEntry(layoutIds[selectedLayout]) : null;

    private void Start()
    {
        if (monsterPrefab == null || spawnRoot == null)
        {
            Debug.LogError("[SpawnLayoutPreview] 몬스터 프리팹 또는 스폰 기준이 연결되지 않았습니다", this);
            return;
        }

        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MonsterDisplayDatabase.Reload();
        LoadData();
        BuildGuides();
        BuildSlots();
        SelectLayout(0);
    }

    private void LoadData()
    {
        SpawnLayoutDatabase.Reload();
        data = SpawnLayoutDatabase.Data;
        data.layouts ??= new List<SpawnLayoutEntry>();

        layoutIds.Clear();
        foreach (EncounterDefinition encounter in MonsterDatabase.AllEncounters)
        {
            if (encounter != null && !string.IsNullOrEmpty(encounter.id))
            {
                layoutIds.Add(encounter.id);
            }
        }

        layoutIds.Add(SpawnLayoutData.RankingScareCrowId);
        dirty = false;
    }

    // 높이 기준선은 spawnRoot 기준, 화면 전체 폭
    private void BuildGuides()
    {
        float spawnBottom = -spawnerSize.y * spawnerPivot.y;
        float ground = spawnBottom + MonsterDisplayDatabase.Display.groundOffset;
        CreateLine("SpawnBottomLine", ScreenWidth, -ScreenWidth * 0.5f, spawnBottom, 2f, spawnLineColor);
        CreateLine("PartyBottomLine", ScreenWidth, -ScreenWidth * 0.5f, spawnBottom + partyBottomFromSpawn, 2f, partyLineColor);
        CreateLine("GroundLine", ScreenWidth, -ScreenWidth * 0.5f, ground, 2f, groundLineColor);
        for (int i = 1; i <= heightGridCount; i++)
        {
            float y = ground + i * heightGridStep;
            CreateLine($"Height{i * heightGridStep:0}", ScreenWidth, -ScreenWidth * 0.5f, y, 1f, heightGridColor);
            CreateText($"HeightLabel{i * heightGridStep:0}", $"{i * heightGridStep:0}", new Vector2(-RegionWidth + 30f, y + 12f), 60f, Color.gray, TextAnchor.MiddleLeft);
        }

        CreateText("Legend", "흰색: 스폰 하단(y 0 기준)  초록: 지면  주황: 파티 하단  회색: 지면 기준 높이  빨강 상자: Idle 그림  초록 짧은 선: 발",
            new Vector2(-ScreenWidth * 0.5f, spawnBottom - 230f), ScreenWidth, Color.gray, TextAnchor.MiddleCenter);
    }

    private void BuildSlots()
    {
        for (int i = 0; i < MaxSlots; i++)
        {
            GameObject monsterObject = Instantiate(monsterPrefab, spawnRoot);
            monsterObject.name = $"Slot {i}";
            RectTransform root = (RectTransform)monsterObject.transform;
            root.anchorMin = root.anchorMax = spawnerAnchor;
            root.pivot = spawnerPivot;
            root.sizeDelta = spawnerSize;

            SpawnLayoutItem item = monsterObject.AddComponent<SpawnLayoutItem>();
            item.Bind(this, i);

            Slot slot = new() { root = root, controller = monsterObject.GetComponent<MonsterStateController>() };
            if (slot.controller != null)
            {
                slot.controller.UpdateIntendIcons(new[] { MonsterIntentIconType.Attack }, 10);
                if (slot.controller.hpSlider != null)
                {
                    slot.controller.hpSlider.interactable = false;
                    slot.controller.hpSlider.maxValue = 50f;
                    slot.controller.hpSlider.value = 50f;
                }

                slot.controller.healthBarView?.Refresh(50, 50, 0);

                if (slot.controller.monsterImage != null)
                {
                    slot.contentBox = new GameObject("ContentBox", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    slot.contentBox.transform.SetParent(slot.controller.monsterImage.transform, false);
                    slot.contentBox.color = contentBoxColor;
                    slot.contentBox.raycastTarget = false;
                }
            }

            slot.feetMark = CreateLine($"Feet {i}", 100f, 0f, 0f, 3f, feetMarkColor);
            slot.label = CreateText($"Label {i}", string.Empty, Vector2.zero, 260f, Color.white, TextAnchor.UpperCenter);
            slot.label.rectTransform.sizeDelta = new Vector2(260f, 70f);
            slots.Add(slot);
        }
    }

    private RectTransform CreateLine(string objectName, float width, float centerX, float y, float thickness, Color color)
    {
        Image line = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        line.transform.SetParent(spawnRoot, false);
        line.color = color;
        line.raycastTarget = false;
        RectTransform rect = line.rectTransform;
        rect.anchorMin = rect.anchorMax = spawnerAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, thickness);
        rect.anchoredPosition = new Vector2(centerX, y);
        return rect;
    }

    private Text CreateText(string objectName, string text, Vector2 position, float width, Color color, TextAnchor alignment)
    {
        Text label = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(spawnRoot, false);
        label.font = labelFont;
        label.fontSize = 18;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = spawnerAnchor;
        rect.sizeDelta = new Vector2(width, 44f);
        rect.anchoredPosition = position;
        return label;
    }

    private void SelectLayout(int index)
    {
        if (layoutIds.Count == 0)
        {
            return;
        }

        selectedLayout = (index % layoutIds.Count + layoutIds.Count) % layoutIds.Count;
        selectedSlot = 0;
        string id = layoutIds[selectedLayout];
        List<string> names = ResolveSlotImages(id, out int startCount);
        SpawnLayoutEntry entry = GetOrCreateEntry(id);
        // 칸이 모자라면 마지막 칸 왼쪽으로 채움
        while (entry.slots.Count < names.Count)
        {
            float x = entry.slots.Count > 0 ? entry.slots[entry.slots.Count - 1].x - 160f : -RegionWidth * 0.5f;
            entry.slots.Add(new SpawnSlot(Mathf.Max(x, -RegionWidth + 1f), 0f));
            dirty = true;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            bool active = i < names.Count;
            slot.root.gameObject.SetActive(active);
            slot.feetMark.gameObject.SetActive(active);
            slot.label.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            slot.imageName = names[i];
            slot.isSummon = i >= startCount;
            if (slot.controller != null)
            {
                slot.controller.SetMonsterImageName(slot.imageName);
                slot.controller.PreviewState(MonsterStateType.Idle);
                if (slot.controller.monsterImage != null)
                {
                    slot.controller.monsterImage.color = slot.isSummon ? summonTint : Color.white;
                }
            }
        }

        RefreshPositions();
    }

    // 칸별 이미지 이름: 조우 몬스터 → 소환 몬스터, 랭킹은 허수아비 1칸
    private static List<string> ResolveSlotImages(string id, out int startCount)
    {
        List<string> names = new();
        startCount = 0;
        if (id == SpawnLayoutData.RankingScareCrowId)
        {
            names.Add(MonsterImagePaths.NormalizeImageName(nameof(ScareCrowMonster)));
            startCount = 1;
            return names;
        }

        EncounterDefinition encounter = MonsterDatabase.GetEncounter(id);
        if (encounter == null)
        {
            return names;
        }

        int summonId = 0;
        foreach (int monsterId in encounter.monsters)
        {
            MonsterDefinition definition = MonsterDatabase.Get(monsterId);
            names.Add(MonsterImagePaths.NormalizeImageName(definition?.className));
            if (summonId == 0 && definition != null && TryFindSummonSpecies(definition, out int species))
            {
                summonId = definition.GetSpeciesIdInSameTier(species);
            }
        }

        startCount = names.Count;
        if (summonId != 0)
        {
            string summonName = MonsterImagePaths.NormalizeImageName(MonsterDatabase.Get(summonId)?.className);
            int maxEnemies = Mathf.Min(MaxSlots, Mathf.Max(0, GameRules.Combat.maxEnemies));
            while (names.Count < maxEnemies)
            {
                names.Add(summonName);
            }
        }

        return names;
    }

    private static bool TryFindSummonSpecies(MonsterDefinition definition, out int species)
    {
        species = 0;
        if (definition.patterns == null)
        {
            return false;
        }

        foreach (MonsterPatternDefinition pattern in definition.patterns)
        {
            if (pattern != null && TryFindSummonSpecies(pattern.actions, out species))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindSummonSpecies(List<MonsterActionDefinition> actions, out int species)
    {
        species = 0;
        if (actions == null)
        {
            return false;
        }

        foreach (MonsterActionDefinition action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (action.type == MonsterActionDefinition.Summon)
            {
                species = action.species;
                return true;
            }

            if (TryFindSummonSpecies(action.then, out species) || TryFindSummonSpecies(action.@else, out species))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshPositions()
    {
        SpawnLayoutEntry entry = CurrentEntry;
        if (entry == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            if (!slot.root.gameObject.activeSelf || i >= entry.slots.Count)
            {
                continue;
            }

            SpawnSlot position = entry.slots[i];
            slot.root.anchoredPosition = new Vector2(position.x, position.y);
            // 같은 조우 안에서 선택한 칸이 위에 그려지도록
            if (i == selectedSlot)
            {
                slot.root.SetAsLastSibling();
            }

            slot.controller?.RefreshImageLayout();
            MeasureSlot(slot);
            UpdateContentBox(slot);
            slot.feetMark.sizeDelta = new Vector2(Mathf.Max(4f, slot.contentRight - slot.contentLeft), 3f);
            slot.feetMark.anchoredPosition = new Vector2((slot.contentLeft + slot.contentRight) * 0.5f, slot.feet);
            slot.label.rectTransform.anchoredPosition = new Vector2(position.x, slot.root.anchoredPosition.y - spawnerSize.y * spawnerPivot.y - 70f);
        }

        CollectWarnings(entry);
        RefreshLabels(entry);
    }

    // 실제 배치된 그림 영역 · 체력 바를 spawnRoot 좌표로 측정
    private void MeasureSlot(Slot slot)
    {
        slot.contentLeft = slot.contentRight = slot.root.anchoredPosition.x;
        slot.feet = slot.root.anchoredPosition.y;
        slot.barLeft = slot.barRight = slot.root.anchoredPosition.x;
        if (slot.controller == null)
        {
            return;
        }

        if (slot.controller.monsterImage != null)
        {
            MonsterImageHitArea hitArea = slot.controller.monsterImage.GetComponent<MonsterImageHitArea>();
            Rect ratio = hitArea != null ? hitArea.ContentRatio : new Rect(0f, 0f, 1f, 1f);
            Rect image = ToSpawnRoot(slot.controller.monsterImage.rectTransform);
            slot.contentLeft = image.xMin + image.width * ratio.xMin;
            slot.contentRight = image.xMin + image.width * ratio.xMax;
            slot.feet = image.yMin + image.height * ratio.yMin;
        }

        if (slot.controller.hpSlider != null)
        {
            Rect bar = ToSpawnRoot((RectTransform)slot.controller.hpSlider.transform);
            slot.barLeft = bar.xMin;
            slot.barRight = bar.xMax;
        }
    }

    // spawnRoot 기준 좌표 (앵커 위치가 원점)
    private Rect ToSpawnRoot(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = spawnRoot.InverseTransformPoint(corners[0]);
        Vector2 max = spawnRoot.InverseTransformPoint(corners[2]);
        Vector2 origin = new Vector2(
            (spawnerAnchor.x - spawnRoot.pivot.x) * spawnRoot.rect.width,
            (spawnerAnchor.y - spawnRoot.pivot.y) * spawnRoot.rect.height);
        return Rect.MinMaxRect(min.x - origin.x, min.y - origin.y, max.x - origin.x, max.y - origin.y);
    }

    private void UpdateContentBox(Slot slot)
    {
        if (slot.contentBox == null)
        {
            return;
        }

        MonsterImageHitArea hitArea = slot.controller.monsterImage.GetComponent<MonsterImageHitArea>();
        Rect ratio = hitArea != null ? hitArea.ContentRatio : new Rect(0f, 0f, 1f, 1f);
        RectTransform box = slot.contentBox.rectTransform;
        box.anchorMin = ratio.min;
        box.anchorMax = ratio.max;
        box.offsetMin = box.offsetMax = Vector2.zero;
        slot.contentBox.enabled = showContentBox;
    }

    private void CollectWarnings(SpawnLayoutEntry entry)
    {
        warnings.Clear();
        for (int i = 0; i < slots.Count && i < entry.slots.Count; i++)
        {
            Slot a = slots[i];
            if (!a.root.gameObject.activeSelf)
            {
                continue;
            }

            if (a.contentLeft < -RegionWidth || a.contentRight > 0f)
            {
                warnings.Add($"{i}번 그림이 오른쪽 절반 밖");
            }

            for (int j = 0; j < i; j++)
            {
                Slot b = slots[j];
                if (!b.root.gameObject.activeSelf)
                {
                    continue;
                }

                if (a.barLeft < b.barRight && b.barLeft < a.barRight
                    && Mathf.Abs(entry.slots[i].y - entry.slots[j].y) < 20f)
                {
                    warnings.Add($"{j}번 · {i}번 체력 바 겹침");
                }

                if (a.contentLeft < b.contentRight && b.contentLeft < a.contentRight)
                {
                    float overlap = Mathf.Min(a.contentRight, b.contentRight) - Mathf.Max(a.contentLeft, b.contentLeft);
                    warnings.Add($"{j}번 · {i}번 그림 겹침 {overlap:0}");
                }
            }
        }
    }

    private void RefreshLabels(SpawnLayoutEntry entry)
    {
        for (int i = 0; i < slots.Count && i < entry.slots.Count; i++)
        {
            Slot slot = slots[i];
            if (!slot.root.gameObject.activeSelf)
            {
                continue;
            }

            SpawnSlot position = entry.slots[i];
            string role = slot.isSummon ? " (소환)" : string.Empty;
            slot.label.text = $"{i}번 {slot.imageName}{role}\nx {position.x:0.#}  y {position.y:0.#}  폭 {slot.contentRight - slot.contentLeft:0}";
            slot.label.color = i == selectedSlot ? selectedLabelColor : Color.white;
        }
    }

    private SpawnLayoutEntry GetOrCreateEntry(string id)
    {
        SpawnLayoutEntry entry = data.Find(id);
        if (entry == null)
        {
            entry = new SpawnLayoutEntry { id = id };
            data.layouts.Add(entry);
            dirty = true;
        }

        entry.slots ??= new List<SpawnSlot>();
        return entry;
    }

    public void SelectSlot(int slotIndex)
    {
        SpawnLayoutEntry entry = CurrentEntry;
        if (entry == null || slotIndex < 0 || slotIndex >= entry.slots.Count)
        {
            return;
        }

        selectedSlot = slotIndex;
        RefreshPositions();
    }

    // delta: 화면 픽셀 이동량
    public void DragSlot(int slotIndex, Vector2 delta)
    {
        SpawnLayoutEntry entry = CurrentEntry;
        if (entry == null || slotIndex < 0 || slotIndex >= entry.slots.Count)
        {
            return;
        }

        Canvas canvas = spawnRoot.GetComponentInParent<Canvas>();
        float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        SpawnSlot position = entry.slots[slotIndex];
        position.x = Mathf.Clamp(Mathf.Round(position.x + delta.x / scale), -RegionWidth + 1f, 0f);
        if (!dragHorizontalOnly)
        {
            position.y = Mathf.Round(position.y + delta.y / scale);
        }

        selectedSlot = slotIndex;
        dirty = true;
        RefreshPositions();
    }

    private void OnGUI()
    {
        if (data == null || slots.Count == 0)
        {
            return;
        }

        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "스폰 배치");
    }

    private void DrawWindow(int windowId)
    {
        bool changed = false;
        string id = layoutIds[selectedLayout];

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ 이전"))
        {
            SelectLayout(selectedLayout - 1);
        }

        GUILayout.Label($"{id}  ({selectedLayout + 1}/{layoutIds.Count})", GUILayout.Width(150f));
        if (GUILayout.Button("다음 ▶"))
        {
            SelectLayout(selectedLayout + 1);
        }

        GUILayout.EndHorizontal();

        listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(200f));
        for (int i = 0; i < layoutIds.Count; i++)
        {
            GUI.color = i == selectedLayout ? selectedLabelColor : Color.white;
            if (GUILayout.Button(BuildListLabel(layoutIds[i])))
            {
                SelectLayout(i);
            }
        }

        GUI.color = Color.white;
        GUILayout.EndScrollView();

        SpawnLayoutEntry entry = CurrentEntry;
        GUILayout.Space(6f);
        int slotCount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].root.gameObject.activeSelf)
            {
                slotCount = i + 1;
            }
        }

        if (slotCount > 0)
        {
            string[] slotNames = new string[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                slotNames[i] = slots[i].isSummon ? $"{i} 소환" : i.ToString();
            }

            int nextSlot = GUILayout.Toolbar(Mathf.Clamp(selectedSlot, 0, slotCount - 1), slotNames);
            if (nextSlot != selectedSlot)
            {
                SelectSlot(nextSlot);
            }

            SpawnSlot position = entry.slots[selectedSlot];
            GUILayout.Label($"{selectedSlot}번 칸: {slots[selectedSlot].imageName}");
            changed |= SliderField("x", ref position.x, -RegionWidth + 1f, 0f);
            changed |= SliderField("y", ref position.y, -300f, 400f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x -10"))
            {
                position.x = Mathf.Max(-RegionWidth + 1f, position.x - 10f);
                changed = true;
            }

            if (GUILayout.Button("x +10"))
            {
                position.x = Mathf.Min(0f, position.x + 10f);
                changed = true;
            }

            if (GUILayout.Button("y 0"))
            {
                position.y = 0f;
                changed = true;
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("조우 전체 좌우 가운데 맞춤 (오른쪽 절반 기준)"))
            {
                CenterGroup(entry, slotCount);
                changed = true;
            }
        }

        dragHorizontalOnly = GUILayout.Toggle(dragHorizontalOnly, "드래그는 가로만 이동");
        bool boxToggle = GUILayout.Toggle(showContentBox, "그림 영역 표시");
        if (boxToggle != showContentBox)
        {
            showContentBox = boxToggle;
            changed = true;
        }

        GUILayout.Space(6f);
        GUI.color = warnings.Count > 0 ? new Color(1f, 0.6f, 0.4f) : Color.white;
        GUILayout.Label(warnings.Count > 0 ? "경고: " + string.Join(", ", warnings) : "경고 없음");
        GUI.color = Color.white;

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(dirty ? "저장 *" : "저장"))
        {
            Save();
        }

        if (GUILayout.Button("다시 읽기 (저장 안 한 값 버림)"))
        {
            LoadData();
            SelectLayout(Mathf.Min(selectedLayout, layoutIds.Count - 1));
            statusMessage = "저장된 값으로 되돌림";
        }

        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Label(statusMessage);
        }

        if (changed)
        {
            dirty = true;
            RefreshPositions();
        }

        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    private static string BuildListLabel(string id)
    {
        if (id == SpawnLayoutData.RankingScareCrowId)
        {
            return $"{id}  허수아비";
        }

        List<string> names = ResolveSlotImages(id, out int startCount);
        if (names.Count > startCount)
        {
            names.RemoveRange(startCount, names.Count - startCount);
            return $"{id}  {string.Join(" · ", names)} + 소환";
        }

        return $"{id}  {string.Join(" · ", names)}";
    }

    // 그림 묶음 전체를 오른쪽 절반 가운데로 이동 (간격 유지)
    private void CenterGroup(SpawnLayoutEntry entry, int slotCount)
    {
        float left = float.MaxValue;
        float right = float.MinValue;
        for (int i = 0; i < slotCount; i++)
        {
            left = Mathf.Min(left, slots[i].contentLeft);
            right = Mathf.Max(right, slots[i].contentRight);
        }

        float shift = Mathf.Round(-RegionWidth * 0.5f - (left + right) * 0.5f);
        for (int i = 0; i < slotCount; i++)
        {
            entry.slots[i].x = Mathf.Clamp(entry.slots[i].x + shift, -RegionWidth + 1f, 0f);
        }
    }

    // 슬라이더 + 숫자 입력, 값이 바뀌면 true
    private static bool SliderField(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(30f));
        float slider = Mathf.Round(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(220f)));
        string text = GUILayout.TextField(value.ToString("0.#"), GUILayout.Width(70f));
        GUILayout.EndHorizontal();

        float next = slider;
        if (Mathf.Approximately(slider, Mathf.Round(value)) && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float typed))
        {
            next = typed;
        }

        if (Mathf.Approximately(next, value))
        {
            return false;
        }

        value = next;
        return true;
    }

    private void Save()
    {
#if UNITY_EDITOR
        // 조우 데이터 순서로 정렬, 모르는 항목은 뒤에 유지
        List<SpawnLayoutEntry> ordered = new();
        foreach (string id in layoutIds)
        {
            SpawnLayoutEntry entry = data.Find(id);
            if (entry != null)
            {
                ordered.Add(entry);
            }
        }

        foreach (SpawnLayoutEntry entry in data.layouts)
        {
            if (entry != null && !ordered.Contains(entry))
            {
                ordered.Add(entry);
            }
        }

        data.layouts = ordered;
        string path = Path.Combine(Application.dataPath, "Resources", SpawnLayoutData.ResourcePath + ".json");
        File.WriteAllText(path, data.ToJson(), new UTF8Encoding(false));
        UnityEditor.AssetDatabase.Refresh();
        dirty = false;
        statusMessage = $"저장 완료: {System.DateTime.Now:HH:mm:ss}";
        Debug.Log($"[SpawnLayoutPreview] 스폰 좌표 저장 → {path}");
#else
        statusMessage = "저장은 에디터에서만 가능";
#endif
    }
}
