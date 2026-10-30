using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime map presenter for training mode.
/// Creates/selects map nodes and enters battle scene.
/// </summary>
public class TrainingMapController : MonoBehaviour
{
    [Header("Node UI")]
    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private RectTransform connectionRoot;
    [Tooltip("Content 안의 지도 배경 (Content를 채워 지도 길이만큼 늘어나고 노드와 함께 스크롤, 연결선 · 노드보다 뒤에 그림)")]
    [SerializeField] private RectTransform scrollBackground;
    [SerializeField] private Button monsterNodeButtonPrefab;
    [SerializeField] private Button namedNodeButtonPrefab;
    [SerializeField] private Button restNodeButtonPrefab;
    [SerializeField] private Button bossNodeButtonPrefab;
    [SerializeField] private RectTransform[] positionTemplates = new RectTransform[10];
    [SerializeField] private float floorSpacing = 200f;
    [Tooltip("첫 층 기준점을 정하는 16:9 화면 폭 (21:9에서 넓어진 폭은 오른쪽 층을 더 보여주는 데 사용)")]
    [SerializeField] private float referenceViewportWidth = 1920f;
    // 지도 · 휴식이 열려 있는 동안 입력을 끄는 전투 영역 (상단 바 · 팝업은 제외)
    [Tooltip("CombatCanvas/BattleField의 CanvasGroup (상단 바 · 팝업이 포함되지 않은 전투 영역)")]
    [SerializeField] private CanvasGroup combatInteraction;
    [SerializeField] private TrainingBattleManager battleManager;
    [Tooltip("휴식 · 탈출 이벤트 노드를 고르면 여는 전체 화면 패널")]
    [SerializeField] private RestPanelController restPanel;

    [Header("Visuals")]
    [SerializeField] private Color selectableNodeColor = new Color(0.26f, 0.73f, 0.29f);
    [SerializeField] private Color lockedNodeColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color clearedNodeColor = new Color(0.2f, 0.45f, 0.8f);
    [SerializeField] private Color currentNodeColor = new Color(1f, 0.84f, 0.2f);
    [SerializeField] private Color selectableConnectionColor = new Color(0.35f, 0.8f, 0.42f, 0.95f);
    [SerializeField] private Color lockedConnectionColor = new Color(0.28f, 0.28f, 0.28f, 0.9f);
    [SerializeField] private Color clearedConnectionColor = new Color(0.3f, 0.56f, 0.9f, 0.95f);
    [SerializeField] private float connectionThickness = 8f;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private Vector2 nodeAnchor;
    // 노드를 배치할 때의 화면 폭 (해상도가 바뀌면 다시 배치)
    private float builtViewportWidth;
    private Vector2 layoutNodeSize;
    private readonly Dictionary<int, Button> nodeButtons = new Dictionary<int, Button>();
    private readonly Dictionary<(int from, int to), Image> connectionImages = new Dictionary<(int, int), Image>();

    private bool isPreview;
    public bool IsVisible => gameObject.activeInHierarchy;
    private bool IsRestOpen => restPanel != null && restPanel.IsOpen;
    // 지도 또는 휴식 패널이 전투 영역을 덮고 있는지 (전투 조작 차단 판정)
    public bool CoversBattle => IsVisible || IsRestOpen;
    private int? DisplayedCurrentNode => isPreview ? TrainingRunState.PendingNodeId : TrainingRunState.CurrentNodeId;

    // 지도 버튼: 미리보기가 열려 있으면 닫기
    public void OpenMapPreview()
    {
        if (isPreview && IsVisible)
        {
            ClosePreview();
            return;
        }
        if (IsVisible) return;
        // 전투 종료 후(보상 진행 중)에는 막고, 휴식 중에는 휴식 위에 미리보기
        if (BossModeSession.IsActive || !TrainingRunState.PendingNodeId.HasValue
            || (battleManager.CurrentTurnState == BattleTurnState.CombatEnd && !IsRestOpen)) return;
        isPreview = true;
        DisplayMap();
        // 미리보기는 일반 패널 (ESC로 닫힘), 전투 후 노드 선택 지도는 기본 화면이라 등록하지 않음
        UIModalStack.Register(new UIModalEntry(this, UIModalKind.Normal, ClosePreview));
        SetSelectionHiddenForPreview(true);
        SettingsManager.PlayPanelToggleSound();
    }

    private void ClosePreview()
    {
        if (IsRestOpen)
        {
            // 휴식 중 미리보기는 지도만 닫고 휴식 화면으로 복귀 (전투 영역 입력은 계속 꺼 둠)
            isPreview = false;
            UIModalStack.Unregister(this);
            SetSelectionHiddenForPreview(false);
            gameObject.SetActive(false);
        }
        else
        {
            ShowBattle();
        }
        SettingsManager.PlayPanelToggleSound();
    }

    // 선택 패널이 지도보다 위에 그려지므로 미리보기 동안 숨김
    private void SetSelectionHiddenForPreview(bool hidden)
    {
        BattleDeckViewer deckViewer = battleManager != null ? battleManager.DeckViewer : null;
        if (deckViewer != null) deckViewer.SetHiddenForMapPreview(hidden);
    }

    // 씬을 다시 불러왔을 때 대기 중인 노드가 휴식 · 이벤트면 전투 대신 휴식 패널을 엶
    public bool TryOpenPendingRest()
    {
        if (restPanel == null || !TrainingRunState.PendingNodeId.HasValue
            || !TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData node)
            || !RestPanelController.IsRestNode(node))
            return false;

        OpenRestPanel(node);
        return true;
    }

    private void OpenRestPanel(TrainingMapNodeData node)
    {
        if (!restPanel.Open(node, this, battleManager)) return;
        isPreview = false;
        UIModalStack.Unregister(this);
        SetSelectionHiddenForPreview(false);
        gameObject.SetActive(false);
        combatInteraction.interactable = false;
        combatInteraction.blocksRaycasts = false;
    }

    private void Awake()
    {
        BindWheelRouter();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshLocalizedTexts;
    }

    // 휠을 아래로 굴리면 지도가 오른쪽(다음 층)으로 이동하도록 휠 입력을 가로 스크롤로 전달
    private void BindWheelRouter()
    {
        ScrollRect scrollRect = nodeRoot != null ? nodeRoot.GetComponentInParent<ScrollRect>(true) : null;
        // ScrollRect와 같은 오브젝트에 붙이면 휠 입력이 두 번 처리되므로 Viewport에만 붙임
        if (scrollRect == null || scrollRect.viewport == null || scrollRect.viewport == scrollRect.transform)
            return;

        GameObject wheelTarget = scrollRect.viewport.gameObject;
        ScrollRectWheelRouter wheelRouter = wheelTarget.GetComponent<ScrollRectWheelRouter>();
        if (wheelRouter == null)
            wheelRouter = wheelTarget.AddComponent<ScrollRectWheelRouter>();
        wheelRouter.Bind(scrollRect);
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshLocalizedTexts;
        UIModalStack.Unregister(this);
    }

    // 언어 변경 시 노드 이름 · 상태 문구 갱신
    private void RefreshLocalizedTexts()
    {
        foreach (var entry in nodeButtons)
        {
            if (TrainingRunState.TryGetNode(entry.Key, out TrainingMapNodeData node))
            {
                ApplyNodeLabel(entry.Value, node);
            }
        }

        UpdateStatusText();
    }

    public void ShowMap()
    {
        isPreview = false;
        UIModalStack.Unregister(this);
        SetSelectionHiddenForPreview(false);
        DisplayMap();
    }

    private void DisplayMap()
    {
        gameObject.SetActive(true);
        combatInteraction.interactable = false;
        combatInteraction.blocksRaycasts = false;
        if (nodeButtons.Count == 0 || !Mathf.Approximately(builtViewportWidth, GetViewportWidth())) BuildMapUI();
        else
        {
            foreach (var entry in nodeButtons)
            {
                bool selectable = TrainingRunState.IsNodeSelectable(entry.Key);
                entry.Value.interactable = !isPreview && selectable;
                ApplyNodeVisual(entry.Value, selectable, TrainingRunState.IsNodeCleared(entry.Key),
                    DisplayedCurrentNode == entry.Key);
            }
            foreach (var entry in connectionImages)
                entry.Value.color = ResolveConnectionColor(entry.Key.from, entry.Key.to);
            UpdateStatusText();
            ResetScrollPosition();
        }
    }

    public void ShowBattle()
    {
        isPreview = false;
        UIModalStack.Unregister(this);
        SetSelectionHiddenForPreview(false);
        combatInteraction.interactable = true;
        combatInteraction.blocksRaycasts = true;
        gameObject.SetActive(false);
        battleManager.UpdateEndTurnButtonState();
        battleManager.RefreshHandPlayableState();
    }

    [ContextMenu("Build Map UI")]
    public void BuildMapUI()
    {
        if (nodeRoot == null || positionTemplates == null || positionTemplates.Length != 10
            || System.Array.Exists(positionTemplates, template => template == null))
        {
            Debug.LogError("[TrainingMapController] Content와 기준 버튼 10개를 인스펙터에 연결하세요");
            return;
        }
        foreach (RectTransform template in positionTemplates) template.gameObject.SetActive(false);
        EnsureConnectionRoot();
        ClearSpawnedNodes();
        ClearSpawnedConnections();

        IReadOnlyList<TrainingMapNodeData> nodes = TrainingRunState.GetAllNodes();
        Canvas.ForceUpdateCanvases();
        UpdateLayoutMetrics(nodes);

        Dictionary<int, Vector2> nodePositions = BuildNodePositions(nodes);
        SpawnConnectionLines(nodes, nodePositions);
        SpawnNodeButtons(nodes, nodePositions);

        UpdateStatusText();
        ResetScrollPosition();
    }

    public void OnNodeSelected(int nodeId)
    {
        if (isPreview || !IsVisible) return;
        if (!TrainingRunState.TrySelectNode(nodeId, out TrainingMapNodeData node))
            return;

        Debug.Log($"[TrainingMapController] Node selected: {node.nodeId} ({node.nodeType})");

        if (RestPanelController.IsRestNode(node))
        {
            if (restPanel == null)
            {
                Debug.LogWarning("[TrainingMapController] 휴식 패널이 연결되지 않아 노드를 바로 완료합니다");
                CompleteNodeOnMap();
                return;
            }

            OpenRestPanel(node);
            return;
        }

        if (!TrainingNodeTypeUtility.RequiresBattle(node.nodeType))
        {
            CompleteNodeOnMap();
            return;
        }

        battleManager.BeginSelectedBattle();
    }

    private void EnsureConnectionRoot()
    {
        if (nodeRoot == null)
            return;

        if (connectionRoot == null)
        {
            GameObject connectionRootObject = new GameObject("Connections", typeof(RectTransform));
            connectionRootObject.transform.SetParent(nodeRoot, false);
            connectionRoot = connectionRootObject.GetComponent<RectTransform>();
        }

        connectionRoot.anchorMin = Vector2.zero;
        connectionRoot.anchorMax = Vector2.one;
        connectionRoot.offsetMin = Vector2.zero;
        connectionRoot.offsetMax = Vector2.zero;
        connectionRoot.SetAsFirstSibling();
        // 지도 배경은 연결선보다 뒤
        if (scrollBackground != null && scrollBackground.parent == nodeRoot)
            scrollBackground.SetAsFirstSibling();
    }

    private void SpawnNodeButtons(IReadOnlyList<TrainingMapNodeData> nodes, IReadOnlyDictionary<int, Vector2> nodePositions)
    {
        foreach (TrainingMapNodeData node in nodes)
        {
            SpawnNodeButton(node, nodePositions[node.nodeId]);
        }
    }

    private void SpawnNodeButton(TrainingMapNodeData node, Vector2 anchoredPosition)
    {
        Button button = CreateNodeButtonInstance(node);
        if (button == null)
            return;

        button.name = $"Node_{node.stageIndex}_{node.laneIndex}_{node.nodeType}";

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = nodeAnchor;
            rect.anchorMax = nodeAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = layoutNodeSize;
            rect.anchoredPosition = anchoredPosition;
        }

        bool isSelectable = TrainingRunState.IsNodeSelectable(node.nodeId);
        bool isCleared = TrainingRunState.IsNodeCleared(node.nodeId);
        bool isCurrent = DisplayedCurrentNode == node.nodeId;

        button.interactable = !isPreview && isSelectable;
        button.onClick.RemoveAllListeners();

        int capturedNodeId = node.nodeId;
        button.onClick.AddListener(() => OnNodeSelected(capturedNodeId));

        ApplyNodeLabel(button, node);
        ApplyNodeVisual(button, isSelectable, isCleared, isCurrent);
        nodeButtons[node.nodeId] = button;
    }

    private Dictionary<int, Vector2> BuildNodePositions(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (TrainingMapNodeData node in nodes)
        {
            counts.TryGetValue(node.stageIndex, out int count);
            counts[node.stageIndex] = count + 1;
        }
        Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>(nodes.Count);
        foreach (TrainingMapNodeData node in nodes)
        {
            int count = counts[node.stageIndex];
            int templateIndex = count * (count - 1) / 2 + node.laneIndex;
            nodePositions[node.nodeId] = new Vector2(
                positionTemplates[0].anchoredPosition.x + node.stageIndex * floorSpacing,
                positionTemplates[templateIndex].anchoredPosition.y);
        }

        return nodePositions;
    }

    private void SpawnConnectionLines(IReadOnlyList<TrainingMapNodeData> nodes, IReadOnlyDictionary<int, Vector2> nodePositions)
    {
        if (connectionRoot == null)
            return;

        foreach (TrainingMapNodeData node in nodes)
        {
            if (!nodePositions.TryGetValue(node.nodeId, out Vector2 fromPosition))
                continue;

            foreach (int nextNodeId in node.nextNodeIds)
            {
                if (!nodePositions.TryGetValue(nextNodeId, out Vector2 toPosition))
                    continue;

                CreateConnectionLine(node.nodeId, nextNodeId, fromPosition, toPosition);
            }
        }
    }

    private void CreateConnectionLine(int fromNodeId, int toNodeId, Vector2 fromPosition, Vector2 toPosition)
    {
        GameObject lineObject = new GameObject($"Connection_{fromNodeId}_{toNodeId}", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(connectionRoot, false);

        RectTransform rect = lineObject.GetComponent<RectTransform>();
        Vector2 delta = toPosition - fromPosition;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            Destroy(lineObject);
            return;
        }

        rect.anchorMin = nodeAnchor;
        rect.anchorMax = nodeAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(length, Mathf.Max(2f, connectionThickness));
        rect.anchoredPosition = (fromPosition + toPosition) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.color = ResolveConnectionColor(fromNodeId, toNodeId);
        image.raycastTarget = false;
        connectionImages[(fromNodeId, toNodeId)] = image;

    }

    private Color ResolveConnectionColor(int fromNodeId, int toNodeId)
    {
        bool isFromCleared = TrainingRunState.IsNodeCleared(fromNodeId);
        bool isToCleared = TrainingRunState.IsNodeCleared(toNodeId);
        bool isFromCurrent = TrainingRunState.CurrentNodeId.HasValue && TrainingRunState.CurrentNodeId.Value == fromNodeId;
        bool isToSelectable = TrainingRunState.IsNodeSelectable(toNodeId);
        bool isFromStartNode = false;

        if (!TrainingRunState.CurrentNodeId.HasValue
            && TrainingRunState.TryGetNode(fromNodeId, out TrainingMapNodeData fromNode))
        {
            isFromStartNode = fromNode.nodeType == TrainingNodeType.Start;
        }

        if (isFromCleared && isToCleared)
            return clearedConnectionColor;

        if ((isFromCurrent || isFromStartNode) && isToSelectable)
            return selectableConnectionColor;

        return lockedConnectionColor;
    }

    private Button CreateNodeButtonInstance(TrainingMapNodeData node)
    {
        if (nodeRoot == null)
            return null;

        Button nodeButtonPrefab = ResolveNodeButtonPrefab(node);
        if (nodeButtonPrefab == null)
        {
            Debug.LogError($"[TrainingMapController] 노드 프리팹이 설정되지 않았습니다. type={node.nodeType}");
            return null;
        }

        return Instantiate(nodeButtonPrefab, nodeRoot);
    }

    private void ApplyNodeVisual(Button button, bool isSelectable, bool isCleared, bool isCurrent)
    {
        Color targetColor = lockedNodeColor;
        if (isCurrent)
        {
            targetColor = currentNodeColor;
        }
        else if (isCleared)
        {
            targetColor = clearedNodeColor;
        }
        else if (isSelectable)
        {
            targetColor = selectableNodeColor;
        }

        Graphic targetGraphic = button.targetGraphic;
        if (targetGraphic != null)
        {
            targetGraphic.color = targetColor;
            targetGraphic.raycastTarget = isSelectable;
        }

        Graphic[] childGraphics = button.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < childGraphics.Length; i++)
        {
            if (childGraphics[i] == targetGraphic)
                continue;

            childGraphics[i].raycastTarget = false;
        }
    }

    private static void ApplyNodeLabel(Button button, TrainingMapNodeData node)
    {
        if (button == null || node == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        if (node.nodeType == TrainingNodeType.Start)
        {
            label.text = "0F";
        }
        else if (TrainingNodeTypeUtility.TryGetEncounterLabelPrefix(node.nodeType, out string encounterLabelPrefix))
        {
            label.text = BuildEncounterLabel(encounterLabelPrefix, node);
        }
        else if (node.nodeType == TrainingNodeType.Event)
        {
            label.text = GameText.Ui("ui.map.event");
        }
    }

    private static string BuildEncounterLabel(string prefix, TrainingMapNodeData node)
    {
        int encounterIndex = MonsterSpawner.GetEncounterDisplayIndex(node.nodeType, node.FloorNumber, node.plannedEncounter);
        if (encounterIndex <= 0)
        {
            return $"{prefix}(?)";
        }

        return $"{prefix}({encounterIndex})";
    }

    private Button ResolveNodeButtonPrefab(TrainingMapNodeData node)
    {
        switch (node.nodeType)
        {
            case TrainingNodeType.Start:
            case TrainingNodeType.Monster:
                return monsterNodeButtonPrefab;
            case TrainingNodeType.Named:
                return namedNodeButtonPrefab;
            case TrainingNodeType.Rest:
                return restNodeButtonPrefab;
            case TrainingNodeType.Event:
                return namedNodeButtonPrefab != null ? namedNodeButtonPrefab : restNodeButtonPrefab;
            case TrainingNodeType.Boss:
                return bossNodeButtonPrefab;
            default:
                return null;
        }
    }

    private void ClearSpawnedNodes()
    {
        foreach (Button button in nodeButtons.Values)
            if (button != null) Destroy(button.gameObject);
        nodeButtons.Clear();
    }

    private void ClearSpawnedConnections()
    {
        foreach (Image image in connectionImages.Values)
            if (image != null) Destroy(image.gameObject);
        connectionImages.Clear();
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        if (isPreview)
        {
            statusText.text = GameText.Ui("ui.map.status.preview");
            return;
        }
        if (TrainingRunState.IsRunCompleted)
        {
            statusText.text = GameText.Ui("ui.map.status.completed");
            return;
        }

        if (TrainingRunState.IsRunFailed)
        {
            statusText.text = GameText.Ui("ui.map.status.failed");
            return;
        }

        if (TrainingRunState.PendingNodeId.HasValue)
        {
            statusText.text = GameText.Ui("ui.map.status.inBattle");
            return;
        }

        statusText.text = GameText.Ui("ui.map.status.selectNext");
    }

    private void UpdateLayoutMetrics(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        float viewportWidth = GetViewportWidth();
        builtViewportWidth = viewportWidth;
        // 기준 버튼 좌표 원점은 16:9 화면 중앙 (넓은 화면에서도 첫 층 위치는 왼쪽 기준으로 같음)
        float originX = Mathf.Min(viewportWidth, referenceViewportWidth) * 0.5f;
        int lastStage = 0;
        foreach (TrainingMapNodeData node in nodes) lastStage = Mathf.Max(lastStage, node.stageIndex);
        layoutNodeSize = positionTemplates[0].sizeDelta;
        // 오른쪽 끝 여유를 첫 층 왼쪽 여유와 같게 맞춤 (지도 배경 좌우 여백 일치)
        float firstNodeLeft = originX + positionTemplates[0].anchoredPosition.x - layoutNodeSize.x * 0.5f;
        float sideMargin = Mathf.Max(0f, firstNodeLeft);
        float width = Mathf.Max(viewportWidth,
            originX + positionTemplates[0].anchoredPosition.x + lastStage * floorSpacing + layoutNodeSize.x * 0.5f + sideMargin);
        nodeRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        // Content 확장 후에도 기준 버튼의 좌표 원점을 첫 화면 중앙에 유지
        nodeAnchor = new Vector2(originX / width, 0.5f);
    }

    private float GetViewportWidth()
    {
        if (nodeRoot == null) return 0f;
        ScrollRect scroll = nodeRoot.GetComponentInParent<ScrollRect>();
        RectTransform viewport = scroll != null ? scroll.viewport : nodeRoot.parent as RectTransform;
        return viewport != null ? viewport.rect.width : 0f;
    }

    private void ResetScrollPosition()
    {
        if (nodeRoot == null)
            return;

        ScrollRect scrollRect = nodeRoot.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        float stage = 0f;
        if (DisplayedCurrentNode.HasValue
            && TrainingRunState.TryGetNode(DisplayedCurrentNode.Value, out TrainingMapNodeData current))
            stage = current.stageIndex;
        float scrollableWidth = nodeRoot.rect.width - scrollRect.viewport.rect.width;
        scrollRect.horizontalNormalizedPosition = scrollableWidth > 0f
            ? Mathf.Clamp01(stage * floorSpacing / scrollableWidth) : 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CompleteNodeOnMap()
    {
        TrainingRunState.CompletePendingNode(true);
        BuildMapUI();
    }

}
