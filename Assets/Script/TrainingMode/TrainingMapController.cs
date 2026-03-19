using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runtime map presenter for training mode.
/// Creates/selects map nodes and enters battle scene.
/// </summary>
public class TrainingMapController : MonoBehaviour
{
    private static readonly Vector2 defaultNodeSize = new Vector2(110f, 56f);

    [Header("Run Setup")]
    [SerializeField] private string battleSceneName = "TrainingScene";
    [SerializeField] private string restSceneName = "TrainingRestScene";
    [SerializeField] private bool autoStartRunIfMissing = true;
    [SerializeField] private int defaultStageCount = 15;
    [SerializeField] private int defaultLaneCount = 4;

    [Header("Node UI")]
    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private RectTransform connectionRoot;
    [SerializeField] private Button monsterNodeButtonPrefab;
    [SerializeField] private Button namedNodeButtonPrefab;
    [SerializeField] private Button restNodeButtonPrefab;
    [SerializeField] private Button bossNodeButtonPrefab;
    [SerializeField] private Vector2 nodeSpacing = new Vector2(260f, 170f);
    [SerializeField] private Vector2 mapOrigin = new Vector2(0f, -320f);
    [SerializeField] private bool autoFitMapToViewport = true;
    [SerializeField] private float viewportPadding = 80f;

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

    private readonly List<GameObject> spawnedNodeObjects = new List<GameObject>();
    private readonly List<GameObject> spawnedConnectionObjects = new List<GameObject>();
    private Vector2 runtimeNodeSize = defaultNodeSize;

    private void Start()
    {
        EnsureRunState();
        BuildMapUI();
    }

    [ContextMenu("Build Map UI")]
    public void BuildMapUI()
    {
        EnsureNodeRoot();
        EnsureConnectionRoot();
        EnsureNodeLayoutDefaults();
        ApplyAdaptiveLayout();
        ClearSpawnedConnections();
        ClearSpawnedNodes();

        IReadOnlyList<TrainingMapNodeData> nodes = TrainingRunState.GetAllNodes();
        Dictionary<int, Vector2> nodePositions = BuildNodePositions(nodes);
        int selectableCount = 0;
        int firstSelectableStage = -1;
        for (int i = 0; i < nodes.Count; i++)
        {
            if (!TrainingRunState.IsNodeSelectable(nodes[i].nodeId))
                continue;

            selectableCount++;
            if (firstSelectableStage < 0 || nodes[i].stageIndex < firstSelectableStage)
            {
                firstSelectableStage = nodes[i].stageIndex;
            }
        }

        Debug.Log(
            $"[TrainingMapController] BuildMapUI nodes={nodes.Count}, selectable={selectableCount}, firstSelectableStage={firstSelectableStage}, spacing={nodeSpacing}, origin={mapOrigin}");

        SpawnConnectionLines(nodes, nodePositions);

        for (int i = 0; i < nodes.Count; i++)
        {
            TrainingMapNodeData node = nodes[i];
            SpawnNodeButton(node, nodePositions[node.nodeId]);
        }

        UpdateStatusText();
    }

    public void OnNodeSelected(int nodeId)
    {
        if (!TrainingRunState.TrySelectNode(nodeId, out TrainingMapNodeData node))
            return;

        Debug.Log($"[TrainingMapController] Node selected: {node.nodeId} ({node.nodeType})");

        if (node.nodeType == TrainingNodeType.Rest)
        {
            if (string.IsNullOrEmpty(restSceneName))
            {
                Debug.LogWarning("[TrainingMapController] Rest scene is empty. Resolving rest node immediately.");
                TrainingRunState.CompletePendingNode(true);
                BuildMapUI();
                return;
            }

            SceneManager.LoadScene(restSceneName);
            return;
        }

        if (NodeRequiresBattle(node.nodeType))
        {
            if (string.IsNullOrEmpty(TrainingRunState.BattleSceneName))
            {
                Debug.LogError("[TrainingMapController] Battle scene name is empty.");
                return;
            }

            SceneManager.LoadScene(TrainingRunState.BattleSceneName);
            return;
        }

        // Non-battle node: resolve immediately on map.
        TrainingRunState.CompletePendingNode(true);
        BuildMapUI();
    }

    private void EnsureRunState()
    {
        if (TrainingRunState.HasMapData)
            return;

        if (!autoStartRunIfMissing)
            return;

        string mapSceneName = SceneManager.GetActiveScene().name;
        TrainingRunState.StartNewRun(mapSceneName, battleSceneName, defaultStageCount, defaultLaneCount);
    }

    private void EnsureNodeRoot()
    {
        if (nodeRoot != null)
        {
            Image rootImage = nodeRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                // Root panel is visual-only; do not block child button clicks.
                rootImage.raycastTarget = false;
            }
            return;
        }

        nodeRoot = transform as RectTransform;
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

    private void SpawnNodeButton(TrainingMapNodeData node, Vector2 anchoredPosition)
    {
        Button button = CreateNodeButtonInstance(node);
        if (button == null)
            return;

        button.name = $"Node_{node.stageIndex}_{node.laneIndex}_{node.nodeType}";

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = runtimeNodeSize;
            rect.anchoredPosition = anchoredPosition;
        }

        bool isSelectable = TrainingRunState.IsNodeSelectable(node.nodeId);
        bool isCleared = TrainingRunState.IsNodeCleared(node.nodeId);
        bool isCurrent = TrainingRunState.CurrentNodeId.HasValue && TrainingRunState.CurrentNodeId.Value == node.nodeId;

        button.interactable = isSelectable;
        button.onClick.RemoveAllListeners();

        int capturedNodeId = node.nodeId;
        button.onClick.AddListener(() => OnNodeSelected(capturedNodeId));

        ApplyNodeVisual(button, node, isSelectable, isCleared, isCurrent);
        spawnedNodeObjects.Add(button.gameObject);
    }

    private Dictionary<int, Vector2> BuildNodePositions(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>(nodes.Count);
        for (int i = 0; i < nodes.Count; i++)
        {
            TrainingMapNodeData node = nodes[i];
            nodePositions[node.nodeId] = GridToAnchoredPosition(node.gridPosition);
        }

        return nodePositions;
    }

    private void SpawnConnectionLines(IReadOnlyList<TrainingMapNodeData> nodes, IReadOnlyDictionary<int, Vector2> nodePositions)
    {
        if (connectionRoot == null)
            return;

        for (int i = 0; i < nodes.Count; i++)
        {
            TrainingMapNodeData node = nodes[i];
            if (!nodePositions.TryGetValue(node.nodeId, out Vector2 fromPosition))
            {
                continue;
            }

            for (int nextIndex = 0; nextIndex < node.nextNodeIds.Count; nextIndex++)
            {
                int nextNodeId = node.nextNodeIds[nextIndex];
                if (!nodePositions.TryGetValue(nextNodeId, out Vector2 toPosition))
                {
                    continue;
                }

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

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(length, Mathf.Max(2f, connectionThickness));
        rect.anchoredPosition = (fromPosition + toPosition) * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.color = ResolveConnectionColor(fromNodeId, toNodeId);
        image.raycastTarget = false;

        spawnedConnectionObjects.Add(lineObject);
    }

    private Color ResolveConnectionColor(int fromNodeId, int toNodeId)
    {
        bool isFromCleared = TrainingRunState.IsNodeCleared(fromNodeId);
        bool isToCleared = TrainingRunState.IsNodeCleared(toNodeId);
        bool isFromCurrent = TrainingRunState.CurrentNodeId.HasValue && TrainingRunState.CurrentNodeId.Value == fromNodeId;
        bool isFromSelectable = TrainingRunState.IsNodeSelectable(fromNodeId);
        bool isToSelectable = TrainingRunState.IsNodeSelectable(toNodeId);

        if (isFromCleared && isToCleared)
        {
            return clearedConnectionColor;
        }

        if (isFromCurrent || isFromSelectable || isToSelectable)
        {
            return selectableConnectionColor;
        }

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

    private void ApplyNodeVisual(Button button, TrainingMapNodeData node, bool isSelectable, bool isCleared, bool isCurrent)
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

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            if (string.IsNullOrEmpty(tmpLabel.text))
            {
                tmpLabel.text = node.GetShortLabel();
            }
        }

        Text legacyLabel = button.GetComponentInChildren<Text>();
        if (legacyLabel != null)
        {
            if (string.IsNullOrEmpty(legacyLabel.text))
            {
                legacyLabel.text = node.GetShortLabel();
            }
        }
    }

    private Button ResolveNodeButtonPrefab(TrainingMapNodeData node)
    {
        switch (node.nodeType)
        {
            case TrainingNodeType.Monster:
                return monsterNodeButtonPrefab;
            case TrainingNodeType.Named:
                return namedNodeButtonPrefab;
            case TrainingNodeType.Rest:
                return restNodeButtonPrefab;
            case TrainingNodeType.Boss:
                return bossNodeButtonPrefab;
            default:
                return null;
        }
    }

    private Vector2 GridToAnchoredPosition(Vector2 gridPosition)
    {
        return new Vector2(
            mapOrigin.x + gridPosition.x * nodeSpacing.x,
            mapOrigin.y + gridPosition.y * nodeSpacing.y);
    }

    private void ClearSpawnedNodes()
    {
        for (int i = 0; i < spawnedNodeObjects.Count; i++)
        {
            if (spawnedNodeObjects[i] != null)
            {
                Destroy(spawnedNodeObjects[i]);
            }
        }

        spawnedNodeObjects.Clear();
    }

    private void ClearSpawnedConnections()
    {
        for (int i = 0; i < spawnedConnectionObjects.Count; i++)
        {
            if (spawnedConnectionObjects[i] != null)
            {
                Destroy(spawnedConnectionObjects[i]);
            }
        }

        spawnedConnectionObjects.Clear();
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        if (TrainingRunState.IsRunCompleted)
        {
            statusText.text = "훈련모드 완료";
            return;
        }

        if (TrainingRunState.IsRunFailed)
        {
            statusText.text = "훈련모드 실패";
            return;
        }

        if (TrainingRunState.PendingNodeId.HasValue)
        {
            statusText.text = "전투 진행 중";
            return;
        }

        statusText.text = "다음 노드를 선택하세요";
    }

    private void EnsureNodeLayoutDefaults()
    {
        if (Mathf.Abs(nodeSpacing.x) < 1f && Mathf.Abs(nodeSpacing.y) < 1f)
        {
            nodeSpacing = new Vector2(260f, 170f);
            Debug.LogWarning("[TrainingMapController] nodeSpacing was near zero. Reset to default (260, 170).");
        }

        runtimeNodeSize = defaultNodeSize;
    }

    private void ApplyAdaptiveLayout()
    {
        if (!autoFitMapToViewport || nodeRoot == null || !TrainingRunState.HasMapData)
            return;

        Rect viewportRect = nodeRoot.rect;
        if (viewportRect.width < 10f || viewportRect.height < 10f)
            return;

        float clampedPadding = Mathf.Clamp(viewportPadding, 20f, Mathf.Min(viewportRect.width, viewportRect.height) * 0.45f);

        int stageCount = Mathf.Max(2, TrainingRunState.StageCount);
        int laneCount = Mathf.Max(2, TrainingRunState.LaneCount);

        float availableWidth = Mathf.Max(120f, viewportRect.width - clampedPadding * 2f);
        float availableHeight = Mathf.Max(120f, viewportRect.height - clampedPadding * 2f);

        float laneSpan = Mathf.Max(1f, laneCount - 1f);
        float stageSpan = Mathf.Max(1f, stageCount - 1f);

        float suggestedX = availableWidth / laneSpan;
        float suggestedY = availableHeight / stageSpan;

        nodeSpacing.x = Mathf.Min(260f, suggestedX);
        nodeSpacing.y = suggestedY;

        // Keep stage 0 visible at the lower viewport area and scale upward.
        mapOrigin.x = 0f;
        mapOrigin.y = -viewportRect.height * 0.5f + clampedPadding;

        runtimeNodeSize = new Vector2(
            Mathf.Clamp(nodeSpacing.x * 0.4f, 72f, defaultNodeSize.x),
            Mathf.Clamp(nodeSpacing.y * 0.68f, 24f, defaultNodeSize.y));
    }

    private static bool NodeRequiresBattle(TrainingNodeType nodeType)
    {
        return nodeType == TrainingNodeType.Monster
               || nodeType == TrainingNodeType.Named
               || nodeType == TrainingNodeType.Boss;
    }
}
