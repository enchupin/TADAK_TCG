using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 몬스터 크기 비교 씬 (Tools/Monster/몬스터 비교 씬 만들기로 생성)
/// 한 줄에 몬스터 한 종씩 Idle · Attack · Hit를 나란히 실제 프리팹으로 표시
/// 몬스터 공통 값과 상태별 보정(배율 · 좌우 · 높이)을 조정해 상태 간 크기를 맞춤
/// 저장하면 JsonData/monsterDisplay.json에 기록되어 전투에 그대로 반영
/// </summary>
public class MonsterLineupPreview : MonoBehaviour
{
    private static readonly MonsterStateType[] States = { MonsterStateType.Idle, MonsterStateType.Attack, MonsterStateType.Hit };
    private static readonly string[] StateSuffixes = MonsterImagePaths.StateSuffixes;

    [Header("참조")]
    [Tooltip("전투에서 쓰는 몬스터 프리팹 (Monster(Temp))")]
    [SerializeField] private GameObject monsterPrefab;

    [Header("기준선")]
    [Tooltip("몬스터 스폰 위치 하단 기준 플레이어 파티 하단 높이 (씬 생성 시 전투 씬에서 측정)")]
    [SerializeField] private float partyBottomFromSpawn;
    [Tooltip("발 기준 높이 눈금 간격")]
    [SerializeField] private float heightGridStep = 100f;
    [SerializeField] private int heightGridCount = 4;

    [Header("줄 배치")]
    [Tooltip("줄 왼쪽 몬스터 이름 칸 폭")]
    [SerializeField] private float nameColumnWidth = 260f;
    [SerializeField] private float slotSpacing = 420f;
    [SerializeField] private float rowSpacing = 640f;
    [Tooltip("줄마다 지면 위로 비워 둘 높이 (큰 몬스터 · 의도 표시용)")]
    [SerializeField] private float rowHeadroom = 480f;
    [SerializeField] private float margin = 120f;

    [Header("표시 색")]
    [SerializeField] private Color spawnLineColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color feetLineColor = new Color(0.3f, 1f, 0.3f, 0.8f);
    [SerializeField] private Color partyLineColor = new Color(1f, 0.6f, 0.2f, 0.8f);
    [SerializeField] private Color heightGridColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color tickColor = new Color(0.4f, 0.8f, 1f, 0.8f);
    [SerializeField] private Color contentBoxColor = new Color(1f, 0.2f, 0.2f, 0.2f);
    [SerializeField] private Color selectedLabelColor = new Color(1f, 0.9f, 0.3f, 1f);

    private class Slot
    {
        public MonsterStateController controller;
        public Image contentBox;
        public Text label;
    }

    private class Row
    {
        public string imageName;
        public float ground;
        public Text nameLabel;
        public RectTransform feetLine;
        public readonly List<RectTransform> heightLines = new();
        public readonly Slot[] slots = new Slot[States.Length];
    }

    private readonly List<Row> rows = new();
    private RectTransform content;
    private MonsterDisplayData data;
    private Font labelFont;
    private int selectedRow;
    private int selectedState;
    private bool showContentBox = true;
    private float zoom = 0.5f;
    private Vector2 pan;
    private Rect windowRect = new Rect(10f, 10f, 340f, 700f);
    private Vector2 listScroll;
    private string statusMessage = string.Empty;

    private float RowWidth => margin * 2f + nameColumnWidth + States.Length * slotSpacing;

    private void Start()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MonsterLineupPreview] 몬스터 프리팹이 연결되지 않았습니다", this);
            return;
        }

        MonsterDisplayDatabase.Reload();
        data = MonsterDisplayDatabase.Display;
        data.monsters ??= new List<MonsterDisplayEntry>();
        labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildLineup();
        ApplyView();
        RefreshAll();
    }

    private void BuildLineup()
    {
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(transform, false);
        content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);

        // 일반 몬스터 이름순, 보스는 마지막
        List<string> names = new();
        foreach (MonsterDisplayEntry entry in data.monsters)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.image))
            {
                names.Add(entry.image);
            }
        }

        names.Sort((a, b) =>
        {
            bool bossA = a.EndsWith("Boss", System.StringComparison.Ordinal);
            bool bossB = b.EndsWith("Boss", System.StringComparison.Ordinal);
            return bossA != bossB ? bossA.CompareTo(bossB) : string.CompareOrdinal(a, b);
        });

        CreateText("Legend", "흰색: 스폰 하단  초록: 발(지면)  주황: 파티 하단  회색: 발 기준 높이 눈금  파랑: 폭 150 / 300  빨강 상자: 그림 영역",
            new Vector2(RowWidth * 0.5f, -margin * 0.5f), RowWidth, Color.gray, TextAnchor.MiddleCenter);

        for (int i = 0; i < names.Count; i++)
        {
            rows.Add(CreateRow(i, names[i]));
        }

        content.sizeDelta = new Vector2(RowWidth, margin * 2f + rowHeadroom + names.Count * rowSpacing);
    }

    private Row CreateRow(int rowIndex, string imageName)
    {
        float ground = -(margin + rowHeadroom + rowIndex * rowSpacing);
        Row row = new() { imageName = imageName, ground = ground };

        CreateLine($"SpawnLine {imageName}", RowWidth, ground, 2f, spawnLineColor);
        CreateLine($"PartyLine {imageName}", RowWidth, ground + partyBottomFromSpawn, 2f, partyLineColor);
        row.feetLine = CreateLine($"FeetLine {imageName}", RowWidth, ground, 2f, feetLineColor);
        for (int i = 1; i <= heightGridCount; i++)
        {
            row.heightLines.Add(CreateLine($"Height{i * heightGridStep:0} {imageName}", RowWidth, ground, 1f, heightGridColor));
        }

        row.nameLabel = CreateText($"Name {imageName}", imageName, new Vector2(margin + nameColumnWidth * 0.5f, ground + 160f),
            nameColumnWidth, Color.white, TextAnchor.MiddleCenter);

        for (int s = 0; s < States.Length; s++)
        {
            float x = margin + nameColumnWidth + (s + 0.5f) * slotSpacing;
            row.slots[s] = CreateSlot(rowIndex, s, imageName, x, ground);
        }

        return row;
    }

    private Slot CreateSlot(int rowIndex, int stateIndex, string imageName, float x, float ground)
    {
        GameObject monsterObject = Instantiate(monsterPrefab, content);
        monsterObject.name = $"{imageName}_{StateSuffixes[stateIndex]}";
        monsterObject.SetActive(true);
        RectTransform root = (RectTransform)monsterObject.transform;
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        // 스폰 위치 하단이 줄 기준선에 오도록 배치
        root.anchoredPosition = new Vector2(x, ground + root.rect.height * root.pivot.y);

        MonsterLineupItem clickTarget = monsterObject.AddComponent<MonsterLineupItem>();
        clickTarget.Bind(this, rowIndex, stateIndex);

        MonsterStateController controller = monsterObject.GetComponent<MonsterStateController>();
        Slot slot = new() { controller = controller };
        if (controller != null)
        {
            controller.SetMonsterImageName(imageName);
            controller.PreviewState(States[stateIndex]);
            controller.UpdateIntendIcons(new[] { MonsterIntentIconType.Attack }, 10);
            if (controller.hpSlider != null)
            {
                controller.hpSlider.interactable = false;
                controller.hpSlider.maxValue = 50f;
                controller.hpSlider.value = 50f;
            }

            controller.healthBarView?.Refresh(50, 50, 0);

            if (controller.monsterImage != null)
            {
                slot.contentBox = new GameObject("ContentBox", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                slot.contentBox.transform.SetParent(controller.monsterImage.transform, false);
                slot.contentBox.color = contentBoxColor;
                slot.contentBox.raycastTarget = false;
            }
        }

        CreateLine($"Tick150 {monsterObject.name}", 150f, ground - 45f, 3f, tickColor, x);
        CreateLine($"Tick300 {monsterObject.name}", 300f, ground - 53f, 3f, tickColor, x);
        slot.label = CreateText($"Label {monsterObject.name}", string.Empty, new Vector2(x, ground - 95f), slotSpacing - 10f, Color.white, TextAnchor.UpperCenter);
        slot.label.rectTransform.sizeDelta = new Vector2(slotSpacing - 10f, 70f);
        return slot;
    }

    private RectTransform CreateLine(string objectName, float width, float y, float thickness, Color color, float? centerX = null)
    {
        Image line = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        line.transform.SetParent(content, false);
        line.color = color;
        line.raycastTarget = false;
        RectTransform rect = line.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, thickness);
        rect.anchoredPosition = new Vector2(centerX ?? width * 0.5f, y);
        return rect;
    }

    private Text CreateText(string objectName, string text, Vector2 position, float width, Color color, TextAnchor alignment)
    {
        Text label = new GameObject(objectName, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        label.transform.SetParent(content, false);
        label.font = labelFont;
        label.fontSize = 18;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(width, 44f);
        rect.anchoredPosition = position;
        return label;
    }

    public void Select(int rowIndex, int stateIndex)
    {
        selectedRow = Mathf.Clamp(rowIndex, 0, Mathf.Max(0, rows.Count - 1));
        selectedState = Mathf.Clamp(stateIndex, 0, States.Length - 1);
        RefreshLabels();
    }

    // 선택한 줄이 화면 위쪽에 오도록 이동
    private void ScrollToRow(int rowIndex)
    {
        pan.y = Mathf.Max(0f, (rowIndex * rowSpacing) * zoom);
        ApplyView();
    }

    private void ApplyView()
    {
        if (content == null)
        {
            return;
        }

        content.localScale = new Vector3(zoom, zoom, 1f);
        content.anchoredPosition = pan;
    }

    private void RefreshAll()
    {
        MonsterDisplayDatabase.SetDisplay(data);
        foreach (Row row in rows)
        {
            foreach (Slot slot in row.slots)
            {
                if (slot.controller == null)
                {
                    continue;
                }

                slot.controller.RefreshImageLayout();
                if (slot.contentBox != null)
                {
                    MonsterImageHitArea hitArea = slot.controller.monsterImage.GetComponent<MonsterImageHitArea>();
                    Rect ratio = hitArea != null ? hitArea.ContentRatio : new Rect(0f, 0f, 1f, 1f);
                    RectTransform box = slot.contentBox.rectTransform;
                    box.anchorMin = ratio.min;
                    box.anchorMax = ratio.max;
                    box.offsetMin = box.offsetMax = Vector2.zero;
                    slot.contentBox.enabled = showContentBox;
                }
            }

            MonsterDisplayEntry entry = data.Find(row.imageName);
            float feet = row.ground + data.groundOffset + (entry?.offsetY ?? 0f);
            SetLineY(row.feetLine, feet);
            for (int i = 0; i < row.heightLines.Count; i++)
            {
                SetLineY(row.heightLines[i], feet + (i + 1) * heightGridStep);
            }
        }

        RefreshLabels();
    }

    private static void SetLineY(RectTransform line, float y)
    {
        Vector2 position = line.anchoredPosition;
        position.y = y;
        line.anchoredPosition = position;
    }

    private void RefreshLabels()
    {
        for (int r = 0; r < rows.Count; r++)
        {
            Row row = rows[r];
            MonsterDisplayEntry entry = GetOrCreateEntry(row.imageName);
            row.nameLabel.text = $"{row.imageName}\n공통 ×{entry.scale:0.##}  좌우 {entry.offsetX:0.#}  띄움 {entry.offsetY:0.#}";
            row.nameLabel.color = r == selectedRow ? selectedLabelColor : Color.white;

            for (int s = 0; s < States.Length; s++)
            {
                Slot slot = row.slots[s];
                if (slot.label == null)
                {
                    continue;
                }

                slot.label.text = BuildSlotText(row.imageName, s, entry);
                slot.label.color = r == selectedRow && s == selectedState ? selectedLabelColor : Color.white;
            }
        }
    }

    // 상태 이름 · 화면상 그림 폭×높이 · 캔버스 크기 · 상태 보정
    private string BuildSlotText(string imageName, int stateIndex, MonsterDisplayEntry entry)
    {
        string suffix = StateSuffixes[stateIndex];
        MonsterSpriteBoundsData bounds = MonsterDisplayDatabase.Bounds;
        MonsterSpriteBoundsEntry current = bounds.Find(MonsterImagePaths.BuildResourcePath(imageName, suffix));
        MonsterSpriteBoundsEntry idle = bounds.Find(MonsterImagePaths.BuildResourcePath(imageName, MonsterImagePaths.IdleImageSuffix));
        MonsterDisplayStateEntry state = entry.GetState(suffix);
        string correction = $"상태 ×{state.scale:0.##}  좌우 {state.offsetX:0.#}  높이 {state.offsetY:0.#}";
        if (current == null)
        {
            return $"{suffix}  (측정값 없음)\n{correction}";
        }

        MonsterSpriteLayoutResult layout = MonsterSpriteLayout.Compute(current, idle ?? current, data, entry, suffix);
        return $"{suffix}  그림 {current.w * layout.scale:0}×{current.h * layout.scale:0}  캔버스 {current.width}×{current.height}\n{correction}";
    }

    private MonsterDisplayEntry GetOrCreateEntry(string imageName)
    {
        MonsterDisplayEntry entry = data.Find(imageName);
        if (entry == null)
        {
            entry = new MonsterDisplayEntry { image = imageName };
            data.monsters.Add(entry);
        }

        return entry;
    }

    private void OnGUI()
    {
        if (data == null || content == null)
        {
            return;
        }

        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "몬스터 크기 비교");
    }

    private void DrawWindow(int windowId)
    {
        bool viewChanged = false;
        bool layoutChanged = false;

        GUILayout.Label("보기");
        viewChanged |= SliderField("확대", ref zoom, 0.1f, 1.5f);
        viewChanged |= SliderField("가로 이동", ref pan.x, -content.sizeDelta.x * zoom, 200f);
        viewChanged |= SliderField("세로 이동", ref pan.y, -200f, content.sizeDelta.y * zoom);

        bool boxToggle = GUILayout.Toggle(showContentBox, "그림 영역(클릭 범위) 표시");
        if (boxToggle != showContentBox)
        {
            showContentBox = boxToggle;
            layoutChanged = true;
        }

        GUILayout.Space(6f);
        GUILayout.Label("공통");
        layoutChanged |= SliderField("지면 높이", ref data.groundOffset, -60f, 120f);
        layoutChanged |= SliderField("의도 간격", ref data.intentGap, -40f, 120f);

        if (rows.Count > 0)
        {
            Row row = rows[selectedRow];
            MonsterDisplayEntry entry = GetOrCreateEntry(row.imageName);
            GUILayout.Space(6f);
            GUILayout.Label($"몬스터: {row.imageName} (세 상태 공통)");
            layoutChanged |= SliderField("배율", ref entry.scale, 0.2f, 4f);
            layoutChanged |= SliderField("좌우 보정", ref entry.offsetX, -200f, 200f);
            layoutChanged |= SliderField("띄움 높이", ref entry.offsetY, -60f, 400f);

            GUILayout.Space(4f);
            int stateIndex = GUILayout.Toolbar(selectedState, StateSuffixes);
            if (stateIndex != selectedState)
            {
                Select(selectedRow, stateIndex);
            }

            MonsterDisplayStateEntry state = entry.GetState(StateSuffixes[selectedState]);
            GUILayout.Label($"상태 보정: {StateSuffixes[selectedState]}");
            layoutChanged |= SliderField("상태 배율", ref state.scale, 0.2f, 3f);
            layoutChanged |= SliderField("상태 좌우", ref state.offsetX, -200f, 200f);
            layoutChanged |= SliderField("상태 높이", ref state.offsetY, -100f, 200f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("상태 보정 초기화"))
            {
                state.scale = 1f;
                state.offsetX = 0f;
                state.offsetY = 0f;
                layoutChanged = true;
            }

            if (GUILayout.Button("몬스터 전체 초기화"))
            {
                entry.scale = 1f;
                entry.offsetX = 0f;
                entry.offsetY = 0f;
                entry.idle = new MonsterDisplayStateEntry();
                entry.attack = new MonsterDisplayStateEntry();
                entry.hit = new MonsterDisplayStateEntry();
                layoutChanged = true;
            }

            GUILayout.EndHorizontal();

            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(170f));
            for (int i = 0; i < rows.Count; i++)
            {
                GUI.color = i == selectedRow ? selectedLabelColor : Color.white;
                if (GUILayout.Button(rows[i].imageName))
                {
                    Select(i, selectedState);
                    ScrollToRow(i);
                }
            }

            GUI.color = Color.white;
            GUILayout.EndScrollView();
        }

        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("저장"))
        {
            Save();
        }

        if (GUILayout.Button("다시 읽기 (저장 안 한 값 버림)"))
        {
            MonsterDisplayDatabase.Reload();
            data = MonsterDisplayDatabase.Display;
            data.monsters ??= new List<MonsterDisplayEntry>();
            layoutChanged = true;
            statusMessage = "저장된 값으로 되돌림";
        }

        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(statusMessage))
        {
            GUILayout.Label(statusMessage);
        }

        if (viewChanged)
        {
            ApplyView();
        }

        if (layoutChanged)
        {
            RefreshAll();
        }

        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    // 슬라이더 + 숫자 입력, 값이 바뀌면 true
    private static bool SliderField(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(70f));
        float slider = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150f));
        string text = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(70f));
        GUILayout.EndHorizontal();

        float next = slider;
        if (Mathf.Approximately(slider, value) && float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float typed))
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
        data.monsters.Sort((a, b) => string.CompareOrdinal(a.image, b.image));
        string path = Path.Combine(Application.dataPath, "Resources", MonsterDisplayData.ResourcePath + ".json");
        File.WriteAllText(path, data.ToJson(), new UTF8Encoding(false));
        UnityEditor.AssetDatabase.Refresh();
        statusMessage = $"저장 완료: {System.DateTime.Now:HH:mm:ss}";
        Debug.Log($"[MonsterLineupPreview] 배치 설정 저장 → {path}");
#else
        statusMessage = "저장은 에디터에서만 가능";
#endif
    }
}
