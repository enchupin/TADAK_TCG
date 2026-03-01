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
    private static Font fallbackLegacyFont;
    private static readonly Vector2 defaultNodeSize = new Vector2(110f, 56f);

    [Header("Run Setup")]
    [SerializeField] private string battleSceneName = "TrainingScene";
    [SerializeField] private string restSceneName = "TrainingRestScene";
    [SerializeField] private bool autoStartRunIfMissing = true;
    [SerializeField] private int defaultStageCount = 6;
    [SerializeField] private int defaultLaneCount = 3;

    [Header("Node UI")]
    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private Button nodeButtonPrefab;
    [SerializeField] private Vector2 nodeSpacing = new Vector2(260f, 170f);
    [SerializeField] private Vector2 mapOrigin = new Vector2(0f, -320f);
    [SerializeField] private bool autoFitMapToViewport = true;
    [SerializeField] private float viewportPadding = 80f;

    [Header("Visuals")]
    [SerializeField] private Color selectableNodeColor = new Color(0.26f, 0.73f, 0.29f);
    [SerializeField] private Color lockedNodeColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color clearedNodeColor = new Color(0.2f, 0.45f, 0.8f);
    [SerializeField] private Color currentNodeColor = new Color(1f, 0.84f, 0.2f);

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private readonly List<GameObject> spawnedNodeObjects = new List<GameObject>();
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
        EnsureNodeLayoutDefaults();
        ApplyAdaptiveLayout();
        ClearSpawnedNodes();

        IReadOnlyList<TrainingMapNodeData> nodes = TrainingRunState.GetAllNodes();
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

        for (int i = 0; i < nodes.Count; i++)
        {
            TrainingMapNodeData node = nodes[i];
            SpawnNodeButton(node);
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

    private void SpawnNodeButton(TrainingMapNodeData node)
    {
        Button button = CreateNodeButtonInstance();
        if (button == null)
            return;

        button.name = $"Node_{node.stageIndex}_{node.laneIndex}_{node.nodeType}";

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = runtimeNodeSize;
            rect.anchoredPosition = GridToAnchoredPosition(node.gridPosition);
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

    private Button CreateNodeButtonInstance()
    {
        if (nodeRoot == null)
            return null;

        if (nodeButtonPrefab != null)
        {
            return Instantiate(nodeButtonPrefab, nodeRoot);
        }

        // Fallback runtime button when no prefab is assigned.
        GameObject nodeGo = new GameObject("NodeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        nodeGo.transform.SetParent(nodeRoot, false);

        RectTransform rect = nodeGo.GetComponent<RectTransform>();
        rect.sizeDelta = runtimeNodeSize;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(nodeGo.transform, false);
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelGo.GetComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.font = GetLegacyRuntimeFont();

        return nodeGo.GetComponent<Button>();
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

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = targetColor;
            image.raycastTarget = isSelectable;
        }

        TMP_Text tmpLabel = button.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = node.GetShortLabel();
            tmpLabel.raycastTarget = false;
        }

        Text legacyLabel = button.GetComponentInChildren<Text>();
        if (legacyLabel != null)
        {
            legacyLabel.text = node.GetShortLabel();
            legacyLabel.raycastTarget = false;
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
        return nodeType == TrainingNodeType.Monster || nodeType == TrainingNodeType.Named;
    }

    private static Font GetLegacyRuntimeFont()
    {
        if (fallbackLegacyFont != null)
            return fallbackLegacyFont;

        fallbackLegacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fallbackLegacyFont;
    }
}
