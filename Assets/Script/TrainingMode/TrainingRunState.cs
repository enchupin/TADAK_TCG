using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only state for a single training-mode run.
/// Persists while the game process is alive.
/// </summary>
public static class TrainingRunState
{
    private const int DefaultFloorCount = 15;
    private const int DefaultMaxNodesPerFloor = 4;
    private const int DefaultInteriorNodeTargetCount = 45;
    private const int DefaultInteriorNodeBaseline = 3;
    private const int MinInteriorNodesPerFloor = 2;

    private static readonly Dictionary<int, TrainingMapNodeData> nodesById = new Dictionary<int, TrainingMapNodeData>();
    private static readonly List<TrainingMapNodeData> orderedNodes = new List<TrainingMapNodeData>();
    private static readonly HashSet<int> selectableNodeIds = new HashSet<int>();
    private static readonly HashSet<int> clearedNodeIds = new HashSet<int>();
    private static readonly Dictionary<int, List<int>> validConnectionMasksByShape = new Dictionary<int, List<int>>();

    public static bool IsRunActive { get; private set; }
    public static bool IsRunCompleted { get; private set; }
    public static bool IsRunFailed { get; private set; }

    public static string MapSceneName { get; private set; } = string.Empty;
    public static string BattleSceneName { get; private set; } = string.Empty;

    public static int StageCount { get; private set; }
    public static int LaneCount { get; private set; }

    public static int? CurrentNodeId { get; private set; }
    public static int? PendingNodeId { get; private set; }

    public static bool HasPlayerHealthState { get; private set; }
    public static int PlayerCurrentHp { get; private set; }
    public static int PlayerMaxHp { get; private set; }

    public static bool HasMapData => orderedNodes.Count > 0;

    public static IReadOnlyList<TrainingMapNodeData> GetAllNodes()
    {
        return orderedNodes;
    }

    public static bool IsNodeSelectable(int nodeId)
    {
        return selectableNodeIds.Contains(nodeId);
    }

    public static bool IsNodeCleared(int nodeId)
    {
        return clearedNodeIds.Contains(nodeId);
    }

    public static bool TryGetNode(int nodeId, out TrainingMapNodeData node)
    {
        return nodesById.TryGetValue(nodeId, out node);
    }

    public static void SetPlayerHealthState(int currentHp, int maxHp)
    {
        PlayerMaxHp = Mathf.Max(1, maxHp);
        PlayerCurrentHp = Mathf.Clamp(currentHp, 0, PlayerMaxHp);
        HasPlayerHealthState = true;
    }

    public static bool TryGetPlayerHealthState(out int currentHp, out int maxHp)
    {
        currentHp = 0;
        maxHp = 0;

        if (!HasPlayerHealthState)
            return false;

        currentHp = PlayerCurrentHp;
        maxHp = PlayerMaxHp;
        return true;
    }

    public static void StartNewRun(string mapSceneName, string battleSceneName, int stageCount = DefaultFloorCount, int laneCount = DefaultMaxNodesPerFloor)
    {
        MapSceneName = mapSceneName;
        BattleSceneName = battleSceneName;

        StageCount = Mathf.Max(2, stageCount);
        LaneCount = Mathf.Clamp(laneCount, 2, DefaultMaxNodesPerFloor);

        BuildSimpleMap(StageCount, LaneCount);

        selectableNodeIds.Clear();
        clearedNodeIds.Clear();

        // First stage is always selectable at run start.
        foreach (TrainingMapNodeData node in orderedNodes)
        {
            if (node.stageIndex == 0)
            {
                selectableNodeIds.Add(node.nodeId);
            }
        }

        CurrentNodeId = null;
        PendingNodeId = null;

        HasPlayerHealthState = false;
        PlayerCurrentHp = 0;
        PlayerMaxHp = 0;

        IsRunCompleted = false;
        IsRunFailed = false;
        IsRunActive = true;

        Debug.Log($"[TrainingRunState] New run started. StageCount={StageCount}, LaneCount={LaneCount}");
    }

    public static bool TrySelectNode(int nodeId, out TrainingMapNodeData selectedNode)
    {
        selectedNode = null;

        if (!IsRunActive)
            return false;

        if (!selectableNodeIds.Contains(nodeId))
            return false;

        if (!nodesById.TryGetValue(nodeId, out selectedNode))
            return false;

        PendingNodeId = nodeId;
        return true;
    }

    public static void CompletePendingNode(bool isVictory)
    {
        if (!PendingNodeId.HasValue)
        {
            Debug.LogWarning("[TrainingRunState] No pending node to resolve.");
            return;
        }

        int resolvedNodeId = PendingNodeId.Value;
        PendingNodeId = null;

        if (!nodesById.TryGetValue(resolvedNodeId, out TrainingMapNodeData resolvedNode))
        {
            Debug.LogWarning($"[TrainingRunState] Pending node {resolvedNodeId} is invalid.");
            return;
        }

        if (!isVictory)
        {
            IsRunFailed = true;
            IsRunActive = false;
            Debug.Log("[TrainingRunState] Run failed.");
            return;
        }

        clearedNodeIds.Add(resolvedNodeId);
        CurrentNodeId = resolvedNodeId;

        selectableNodeIds.Clear();
        foreach (int nextNodeId in resolvedNode.nextNodeIds)
        {
            selectableNodeIds.Add(nextNodeId);
        }

        if (selectableNodeIds.Count == 0)
        {
            IsRunCompleted = true;
            IsRunActive = false;
            Debug.Log("[TrainingRunState] Run completed.");
        }
    }

    public static void EndRun(bool clearMap = false)
    {
        IsRunActive = false;
        PendingNodeId = null;

        if (!clearMap)
            return;

        MapSceneName = string.Empty;
        BattleSceneName = string.Empty;
        StageCount = 0;
        LaneCount = 0;
        CurrentNodeId = null;

        nodesById.Clear();
        orderedNodes.Clear();
        selectableNodeIds.Clear();
        clearedNodeIds.Clear();

        IsRunCompleted = false;
        IsRunFailed = false;

        HasPlayerHealthState = false;
        PlayerCurrentHp = 0;
        PlayerMaxHp = 0;
    }

    private static void BuildSimpleMap(int stageCount, int laneCount)
    {
        nodesById.Clear();
        orderedNodes.Clear();

        List<int> stageNodeCounts = BuildStageNodeCounts(stageCount, laneCount);
        List<List<int>> stageNodeIds = new List<List<int>>();
        int nextId = 0;

        for (int stage = 0; stage < stageCount; stage++)
        {
            int nodeCount = stageNodeCounts[stage];
            List<int> idsInStage = new List<int>();
            float centeredOffset = (nodeCount - 1) * 0.5f;
            for (int lane = 0; lane < nodeCount; lane++)
            {
                TrainingMapNodeData node = new TrainingMapNodeData
                {
                    nodeId = nextId++,
                    stageIndex = stage,
                    laneIndex = lane,
                    nodeType = DetermineNodeType(stage, lane, stageCount, nodeCount),
                    gridPosition = new Vector2(lane - centeredOffset, stage)
                };

                nodesById[node.nodeId] = node;
                orderedNodes.Add(node);
                idsInStage.Add(node.nodeId);
            }
            stageNodeIds.Add(idsInStage);
        }

        for (int stage = 0; stage < stageNodeIds.Count - 1; stage++)
        {
            ApplyRandomStageConnections(stageNodeIds[stage], stageNodeIds[stage + 1]);
        }
    }

    private static List<int> BuildStageNodeCounts(int stageCount, int laneCount)
    {
        List<int> stageNodeCounts = new List<int>(stageCount);
        if (stageCount <= 0)
        {
            return stageNodeCounts;
        }

        stageNodeCounts.Add(1);

        int interiorStageCount = Mathf.Max(0, stageCount - 2);
        if (interiorStageCount > 0)
        {
            List<int> interiorCounts = new List<int>(interiorStageCount);
            int baselineCount = Mathf.Clamp(DefaultInteriorNodeBaseline, MinInteriorNodesPerFloor, laneCount);
            for (int i = 0; i < interiorStageCount; i++)
            {
                interiorCounts.Add(baselineCount);
            }

            int targetInteriorNodeCount = Mathf.Clamp(
                DefaultInteriorNodeTargetCount,
                interiorStageCount * MinInteriorNodesPerFloor,
                interiorStageCount * laneCount);
            int currentInteriorNodeCount = baselineCount * interiorStageCount;
            int delta = targetInteriorNodeCount - currentInteriorNodeCount;

            if (delta > 0)
            {
                ApplyStageNodeCountDelta(interiorCounts, delta, 1, laneCount);
            }
            else if (delta < 0)
            {
                ApplyStageNodeCountDelta(interiorCounts, -delta, -1, MinInteriorNodesPerFloor);
            }

            stageNodeCounts.AddRange(interiorCounts);
        }

        if (stageCount > 1)
        {
            stageNodeCounts.Add(1);
        }

        return stageNodeCounts;
    }

    private static void ApplyStageNodeCountDelta(List<int> stageNodeCounts, int deltaCount, int deltaValue, int limit)
    {
        if (stageNodeCounts == null || stageNodeCounts.Count == 0 || deltaCount <= 0)
        {
            return;
        }

        List<int> candidateIndices = new List<int>();
        for (int i = 0; i < stageNodeCounts.Count; i++)
        {
            if (deltaValue > 0)
            {
                if (stageNodeCounts[i] < limit)
                {
                    candidateIndices.Add(i);
                }
            }
            else if (stageNodeCounts[i] > limit)
            {
                candidateIndices.Add(i);
            }
        }

        for (int i = 0; i < deltaCount && candidateIndices.Count > 0; i++)
        {
            int pickedIndex = Random.Range(0, candidateIndices.Count);
            int stageIndex = candidateIndices[pickedIndex];
            candidateIndices.RemoveAt(pickedIndex);
            stageNodeCounts[stageIndex] += deltaValue;
        }
    }

    private static void ApplyRandomStageConnections(List<int> currentStageNodeIds, List<int> nextStageNodeIds)
    {
        if (currentStageNodeIds == null || nextStageNodeIds == null || currentStageNodeIds.Count == 0 || nextStageNodeIds.Count == 0)
        {
            return;
        }

        List<int> validMasks = GetValidConnectionMasks(currentStageNodeIds.Count, nextStageNodeIds.Count);
        int selectedMask = validMasks.Count > 0
            ? validMasks[Random.Range(0, validMasks.Count)]
            : 0;

        for (int currentIndex = 0; currentIndex < currentStageNodeIds.Count; currentIndex++)
        {
            if (!nodesById.TryGetValue(currentStageNodeIds[currentIndex], out TrainingMapNodeData currentNode))
            {
                continue;
            }

            currentNode.nextNodeIds.Clear();
            for (int nextIndex = 0; nextIndex < nextStageNodeIds.Count; nextIndex++)
            {
                if (!HasConnection(selectedMask, currentIndex, nextIndex, nextStageNodeIds.Count))
                {
                    continue;
                }

                currentNode.nextNodeIds.Add(nextStageNodeIds[nextIndex]);
            }
        }
    }

    private static List<int> GetValidConnectionMasks(int currentNodeCount, int nextNodeCount)
    {
        int cacheKey = (currentNodeCount << 8) | nextNodeCount;
        if (validConnectionMasksByShape.TryGetValue(cacheKey, out List<int> cachedMasks))
        {
            return cachedMasks;
        }

        List<int> validMasks = new List<int>();
        int totalEdgeCount = currentNodeCount * nextNodeCount;
        int maxMask = 1 << totalEdgeCount;
        for (int mask = 1; mask < maxMask; mask++)
        {
            if (IsValidConnectionMask(mask, currentNodeCount, nextNodeCount))
            {
                validMasks.Add(mask);
            }
        }

        validConnectionMasksByShape[cacheKey] = validMasks;
        return validMasks;
    }

    private static bool IsValidConnectionMask(int mask, int currentNodeCount, int nextNodeCount)
    {
        int[] outgoingCounts = new int[currentNodeCount];
        int[] incomingCounts = new int[nextNodeCount];

        for (int currentIndex = 0; currentIndex < currentNodeCount; currentIndex++)
        {
            for (int nextIndex = 0; nextIndex < nextNodeCount; nextIndex++)
            {
                if (!HasConnection(mask, currentIndex, nextIndex, nextNodeCount))
                {
                    continue;
                }

                outgoingCounts[currentIndex]++;
                incomingCounts[nextIndex]++;
            }
        }

        for (int currentIndex = 0; currentIndex < currentNodeCount; currentIndex++)
        {
            if (outgoingCounts[currentIndex] <= 0)
            {
                return false;
            }

            if (currentNodeCount > 1 && nextNodeCount > 1 && outgoingCounts[currentIndex] >= nextNodeCount)
            {
                return false;
            }
        }

        for (int nextIndex = 0; nextIndex < nextNodeCount; nextIndex++)
        {
            if (incomingCounts[nextIndex] <= 0)
            {
                return false;
            }

            if (currentNodeCount > 1 && nextNodeCount > 1 && incomingCounts[nextIndex] >= currentNodeCount)
            {
                return false;
            }
        }

        for (int leftCurrentIndex = 0; leftCurrentIndex < currentNodeCount; leftCurrentIndex++)
        {
            for (int rightCurrentIndex = leftCurrentIndex + 1; rightCurrentIndex < currentNodeCount; rightCurrentIndex++)
            {
                for (int leftNextIndex = 0; leftNextIndex < nextNodeCount; leftNextIndex++)
                {
                    if (!HasConnection(mask, leftCurrentIndex, leftNextIndex, nextNodeCount))
                    {
                        continue;
                    }

                    for (int rightNextIndex = 0; rightNextIndex < leftNextIndex; rightNextIndex++)
                    {
                        if (HasConnection(mask, rightCurrentIndex, rightNextIndex, nextNodeCount))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private static bool HasConnection(int mask, int currentIndex, int nextIndex, int nextNodeCount)
    {
        int bitIndex = currentIndex * nextNodeCount + nextIndex;
        return (mask & (1 << bitIndex)) != 0;
    }

    private static TrainingNodeType DetermineNodeType(int stage, int lane, int stageCount, int nodeCountInStage)
    {
        if (stage == stageCount - 1)
            return TrainingNodeType.Named;

        if (stage > 0 && stage % 3 == 0 && lane == nodeCountInStage - 1)
            return TrainingNodeType.Rest;

        if (stage > 0 && (stage + lane) % 4 == 0)
            return TrainingNodeType.Named;

        if (stage == 0)
            return TrainingNodeType.Monster;

        return TrainingNodeType.Monster;
    }
}
