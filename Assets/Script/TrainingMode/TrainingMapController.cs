using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime map presenter for training mode.
/// Creates/selects map nodes and enters battle scene.
/// </summary>
public class TrainingMapController : MonoBehaviour
{
    private static readonly Vector2 defaultNodeSize = new Vector2(110f, 56f);
    private static readonly Vector2 defaultNodeSpacing = new Vector2(260f, 170f);

    [Header("Run Setup")]
    [SerializeField] private string battleSceneName = "TrainingScene";
    [SerializeField] private string restSceneName = "TrainingRestScene";
    [SerializeField] private bool autoStartRunIfMissing = true;

    [Header("Node UI")]
    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private RectTransform connectionRoot;
    [SerializeField] private Button monsterNodeButtonPrefab;
    [SerializeField] private Button namedNodeButtonPrefab;
    [SerializeField] private Button restNodeButtonPrefab;
    [SerializeField] private Button bossNodeButtonPrefab;
    [SerializeField] private Vector2 nodeSpacing = new Vector2(260f, 170f);
    [SerializeField] private Vector2 nodeSize = new Vector2(110f, 56f);
    [SerializeField] private Vector2 mapPadding = new Vector2(80f, 80f);
    [SerializeField] private float startNodeOffset = 1f;
    [SerializeField] private float laneSpacingScale = 0.9f;

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
    private Vector2 layoutOrigin;
    private Vector2 layoutNodeSize = defaultNodeSize;
    private Vector2 effectiveNodeSpacing = defaultNodeSpacing;

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

        if (!NodeRequiresBattle(node.nodeType))
        {
            CompleteNodeOnMap();
            return;
        }

        if (string.IsNullOrEmpty(TrainingRunState.BattleSceneName))
        {
            Debug.LogError("[TrainingMapController] Battle scene name is empty.");
            return;
        }

        SceneManager.LoadScene(TrainingRunState.BattleSceneName);
    }

    private void EnsureRunState()
    {
        if (TrainingRunState.HasMapData)
            return;

        if (!autoStartRunIfMissing)
            return;

        string mapSceneName = SceneManager.GetActiveScene().name;
        TrainingRunState.StartNewRun(mapSceneName, battleSceneName);
    }

    private void EnsureNodeRoot()
    {
        if (nodeRoot == null)
        {
            nodeRoot = transform as RectTransform;
        }

        if (nodeRoot == null)
            return;

        Image rootImage = nodeRoot.GetComponent<Image>();
        if (rootImage != null)
        {
            // Root panel is visual-only; do not block child button clicks.
            rootImage.raycastTarget = false;
        }
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
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = layoutNodeSize;
            rect.anchoredPosition = anchoredPosition;
        }

        bool isSelectable = TrainingRunState.IsNodeSelectable(node.nodeId);
        bool isCleared = TrainingRunState.IsNodeCleared(node.nodeId);
        bool isCurrent = TrainingRunState.CurrentNodeId.HasValue && TrainingRunState.CurrentNodeId.Value == node.nodeId;

        button.interactable = isSelectable;
        button.onClick.RemoveAllListeners();

        int capturedNodeId = node.nodeId;
        button.onClick.AddListener(() => OnNodeSelected(capturedNodeId));

        ApplyNodeVisual(button, isSelectable, isCleared, isCurrent);
        spawnedNodeObjects.Add(button.gameObject);
    }

    private Dictionary<int, Vector2> BuildNodePositions(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        Dictionary<int, Vector2> nodePositions = new Dictionary<int, Vector2>(nodes.Count);
        foreach (TrainingMapNodeData node in nodes)
        {
            nodePositions[node.nodeId] = GridToAnchoredPosition(node.gridPosition);
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

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
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
            return clearedConnectionColor;

        if (isFromCurrent || isFromSelectable || isToSelectable)
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
            layoutOrigin.x + gridPosition.x * effectiveNodeSpacing.x,
            layoutOrigin.y - gridPosition.y * effectiveNodeSpacing.y);
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

    private void UpdateLayoutMetrics(IReadOnlyList<TrainingMapNodeData> nodes)
    {
        Rect layoutRect = ResolveLayoutRect();
        float layoutWidth = Mathf.Max(0f, layoutRect.width);
        float layoutHeight = Mathf.Max(0f, layoutRect.height);

        Vector2 sanitizedSpacing = new Vector2(
            Mathf.Max(1f, nodeSpacing.x),
            Mathf.Max(0f, nodeSpacing.y));
        Vector2 sanitizedNodeSize = new Vector2(
            Mathf.Max(1f, nodeSize.x),
            Mathf.Max(1f, nodeSize.y));
        Vector2 sanitizedPadding = new Vector2(
            Mathf.Max(0f, mapPadding.x),
            Mathf.Max(0f, mapPadding.y));

        layoutNodeSize = sanitizedNodeSize;

        GetGridBounds(nodes, out float maxStage, out float minLane, out float maxLane);

        float scaledSpacingY = sanitizedSpacing.y * Mathf.Max(0f, laneSpacingScale);
        float stageSpan = Mathf.Max(0f, Mathf.Max(0f, startNodeOffset) + maxStage);
        float laneSpan = Mathf.Max(0f, maxLane - minLane);
        float availableWidth = Mathf.Max(0f, layoutWidth - sanitizedPadding.x * 2f - sanitizedNodeSize.x);
        float availableHeight = Mathf.Max(0f, layoutHeight - sanitizedPadding.y * 2f - sanitizedNodeSize.y);

        float fittedSpacingX = stageSpan > 0f
            ? Mathf.Max(0f, availableWidth / stageSpan)
            : sanitizedSpacing.x;
        float fittedSpacingY = laneSpan > 0f
            ? Mathf.Max(0f, availableHeight / laneSpan)
            : scaledSpacingY;

        effectiveNodeSpacing = new Vector2(
            stageSpan > 0f ? Mathf.Min(sanitizedSpacing.x, fittedSpacingX) : sanitizedSpacing.x,
            laneSpan > 0f ? Mathf.Min(scaledSpacingY, fittedSpacingY) : scaledSpacingY);

        layoutOrigin = new Vector2(
            sanitizedPadding.x + sanitizedNodeSize.x * 0.5f + Mathf.Max(0f, startNodeOffset) * effectiveNodeSpacing.x,
            -layoutHeight * 0.5f);
    }

    private void GetGridBounds(IReadOnlyList<TrainingMapNodeData> nodes, out float maxStage, out float minLane, out float maxLane)
    {
        maxStage = 0f;
        minLane = 0f;
        maxLane = 0f;

        if (nodes == null || nodes.Count == 0)
            return;

        maxStage = nodes[0].gridPosition.x;
        minLane = nodes[0].gridPosition.y;
        maxLane = nodes[0].gridPosition.y;

        for (int i = 1; i < nodes.Count; i++)
        {
            Vector2 gridPosition = nodes[i].gridPosition;
            maxStage = Mathf.Max(maxStage, gridPosition.x);
            minLane = Mathf.Min(minLane, gridPosition.y);
            maxLane = Mathf.Max(maxLane, gridPosition.y);
        }
    }

    private Rect ResolveLayoutRect()
    {
        return nodeRoot != null ? nodeRoot.rect : default;
    }

    private void ResetScrollPosition()
    {
        if (nodeRoot == null)
            return;

        ScrollRect scrollRect = nodeRoot.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CompleteNodeOnMap()
    {
        TrainingRunState.CompletePendingNode(true);
        BuildMapUI();
    }

    private static bool NodeRequiresBattle(TrainingNodeType nodeType)
    {
        return nodeType == TrainingNodeType.Monster
               || nodeType == TrainingNodeType.Named
               || nodeType == TrainingNodeType.Boss;
    }
}
