using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime map presenter for training mode.
/// Creates/selects map nodes and enters battle scene.
/// </summary>
public class TrainingMapController : MonoBehaviour
{
    [Header("Run Setup")]
    [SerializeField] private string restSceneName = "TrainingRestScene";
    [SerializeField] private string eventSceneName = "TrainingRestScene";

    [Header("Node UI")]
    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private RectTransform connectionRoot;
    [SerializeField] private Button monsterNodeButtonPrefab;
    [SerializeField] private Button namedNodeButtonPrefab;
    [SerializeField] private Button restNodeButtonPrefab;
    [SerializeField] private Button bossNodeButtonPrefab;
    [SerializeField] private RectTransform[] positionTemplates = new RectTransform[10];
    [SerializeField] private float floorSpacing = 200f;
    [SerializeField] private CanvasGroup combatInteraction;
    [SerializeField] private TrainingBattleManager battleManager;

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
    private Vector2 layoutNodeSize;
    private readonly Dictionary<int, Button> nodeButtons = new Dictionary<int, Button>();
    private readonly Dictionary<(int from, int to), Image> connectionImages = new Dictionary<(int, int), Image>();

    private bool isPreview;
    private static TrainingMapController activePreview;
    private static int escapeConsumedFrame = -1;
    public bool IsVisible => gameObject.activeInHierarchy;
    private int? DisplayedCurrentNode => isPreview ? TrainingRunState.PendingNodeId : TrainingRunState.CurrentNodeId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPreviewState()
    {
        activePreview = null;
        escapeConsumedFrame = -1;
    }

    public void OpenMapPreview()
    {
        if (IsVisible) return;
        if (BossModeSession.IsActive || !TrainingRunState.PendingNodeId.HasValue
            || battleManager.CurrentTurnState == BattleTurnState.CombatEnd) return;
        isPreview = true;
        activePreview = this;
        DisplayMap();
        SettingsManager.PlayPanelToggleSound();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame
            && !(SettingsManager.Instance != null && SettingsManager.Instance.IsSettingsPanelOpen()))
            TryHandlePreviewEscape();
    }

    public static bool TryHandlePreviewEscape(bool settingsPanelOpen = false)
    {
        if (escapeConsumedFrame == Time.frameCount) return true;
        if (settingsPanelOpen)
        {
            escapeConsumedFrame = Time.frameCount;
            return false;
        }
        if (activePreview == null || !activePreview.isPreview || !activePreview.IsVisible) return false;
        escapeConsumedFrame = Time.frameCount;
        activePreview.ShowBattle();
        SettingsManager.PlayPanelToggleSound();
        return true;
    }

    private void OnDisable()
    {
        if (activePreview == this) activePreview = null;
    }

    public void ShowMap()
    {
        isPreview = false;
        if (activePreview == this) activePreview = null;
        DisplayMap();
    }

    private void DisplayMap()
    {
        gameObject.SetActive(true);
        combatInteraction.interactable = false;
        combatInteraction.blocksRaycasts = false;
        if (nodeButtons.Count == 0) BuildMapUI();
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

        if (node.nodeType == TrainingNodeType.Rest)
        {
            if (string.IsNullOrEmpty(restSceneName))
            {
                Debug.LogWarning("[TrainingMapController] Rest scene is empty. Resolving rest node immediately.");
                CompleteNodeOnMap();
                return;
            }

            SceneManager.LoadScene(restSceneName);
            return;
        }

        if (node.nodeType == TrainingNodeType.Event)
        {
            string targetSceneName = string.IsNullOrEmpty(eventSceneName) ? restSceneName : eventSceneName;
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("[TrainingMapController] Event scene is empty. Resolving event node immediately.");
                CompleteNodeOnMap();
                return;
            }

            SceneManager.LoadScene(targetSceneName);
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
            label.text = "이벤트";
        }
    }

    private static string BuildEncounterLabel(string prefix, TrainingMapNodeData node)
    {
        int encounterIndex = MonsterSpawner.GetEncounterDisplayIndex(node.nodeType, node.stageIndex, node.plannedEncounter);
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
            statusText.text = "현재 위치 확인 · ESC로 닫기";
            return;
        }
        if (TrainingRunState.IsRunCompleted)
        {
            statusText.text = "훈련 모드 완료";
            return;
        }

        if (TrainingRunState.IsRunFailed)
        {
            statusText.text = "훈련 모드 실패";
            return;
        }

        if (TrainingRunState.PendingNodeId.HasValue)
        {
            statusText.text = "전투 진행 중";
            return;
        }

        statusText.text = "다음 노드를 선택하세요";
    }

    private void UpdateLayoutMetrics(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        ScrollRect scroll = nodeRoot.GetComponentInParent<ScrollRect>();
        RectTransform viewport = scroll != null ? scroll.viewport : nodeRoot.parent as RectTransform;
        float viewportWidth = viewport.rect.width;
        float originX = viewportWidth * 0.5f;
        int lastStage = 0;
        foreach (TrainingMapNodeData node in nodes) lastStage = Mathf.Max(lastStage, node.stageIndex);
        layoutNodeSize = positionTemplates[0].sizeDelta;
        float width = Mathf.Max(viewportWidth,
            originX + positionTemplates[0].anchoredPosition.x + lastStage * floorSpacing + layoutNodeSize.x * 0.5f + 60f);
        nodeRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        // Content 확장 후에도 기준 버튼의 좌표 원점을 첫 화면 중앙에 유지
        nodeAnchor = new Vector2(originX / width, 0.5f);
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
