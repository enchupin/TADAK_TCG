using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only state for a single training-mode run.
/// Persists while the game process is alive.
/// </summary>
public static class TrainingRunState
{
    private const int DefaultFloorCount = 15;
    private const int TotalStageCount = DefaultFloorCount + 1;
    private const int DefaultMaxNodesPerFloor = 4;
    private const int DefaultTotalNodeCount = 46;
    private const int StartStageIndex = 0;
    private const int FirstCombatStageIndex = 1;
    private const int MidTowerSingleNodeStageIndex = 8;
    private const int BossStageIndex = DefaultFloorCount;
    private const int SingleNodeFloorNodeCount = 1;
    private const int MinNodesPerRegularFloor = 2;

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

    public static void StartNewRun(string mapSceneName, string battleSceneName)
    {
        MapSceneName = mapSceneName;
        BattleSceneName = battleSceneName;

        BuildSimpleMap();

        selectableNodeIds.Clear();
        clearedNodeIds.Clear();

        InitializeRunStartState();

        PendingNodeId = null;

        HasPlayerHealthState = false;
        PlayerCurrentHp = 0;
        PlayerMaxHp = 0;

        IsRunCompleted = false;
        IsRunFailed = false;
        IsRunActive = true;

        Debug.Log("[TrainingRunState] New run started.");
    }

    private static void BuildSimpleMap()
    {
        nodesById.Clear();
        orderedNodes.Clear();

        List<int> stageNodeCounts = BuildStageNodeCounts();
        List<List<int>> stageNodeIds = new List<List<int>>();
        int nextId = 0;

        for (int stage = 0; stage < TotalStageCount; stage++)
        {
            int nodeCount = stageNodeCounts[stage];
            List<int> idsInStage = new List<int>();
            float centeredOffset = (nodeCount - 1) * 0.5f;
            for (int lane = 0; lane < nodeCount; lane++)
            {
                TrainingNodeType nodeType = DetermineNodeType(stage, lane, nodeCount);
                TrainingMapNodeData node = new TrainingMapNodeData
                {
                    nodeId = nextId++,
                    stageIndex = stage,
                    laneIndex = lane,
                    nodeType = nodeType,
                    gridPosition = new Vector2(stage, lane - centeredOffset),
                    plannedEncounter = CreatePlannedEncounter(nodeType, stage)
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

    private static List<MonsterSpawner.SpawnMonsterType> CreatePlannedEncounter(TrainingNodeType nodeType, int stageIndex)
    {
        if (!NodeRequiresBattle(nodeType))
        {
            return new List<MonsterSpawner.SpawnMonsterType>();
        }

        return MonsterSpawner.CreateEncounterPlan(nodeType, stageIndex);
    }

    private static List<int> BuildStageNodeCounts()
    {
        List<int> stageNodeCounts = new List<int>(TotalStageCount);
        HashSet<int> singleNodeStageIndices = BuildSingleNodeStageIndices();

        int currentTotalNodeCount = 0;
        for (int stage = 0; stage < TotalStageCount; stage++)
        {
            int nodeCount = singleNodeStageIndices.Contains(stage)
                ? SingleNodeFloorNodeCount
                : MinNodesPerRegularFloor;
            stageNodeCounts.Add(nodeCount);
            currentTotalNodeCount += nodeCount;
        }

        int additionalNodeCount = DefaultTotalNodeCount - currentTotalNodeCount;
        DistributeAdditionalNodes(stageNodeCounts, singleNodeStageIndices, additionalNodeCount);

        return stageNodeCounts;
    }

    private static HashSet<int> BuildSingleNodeStageIndices()
    {
        return new HashSet<int>
        {
            StartStageIndex,
            MidTowerSingleNodeStageIndex,
            BossStageIndex
        };
    }

    private static void InitializeRunStartState()
    {
        CurrentNodeId = null;

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (node.stageIndex != StartStageIndex)
            {
                continue;
            }

            clearedNodeIds.Add(node.nodeId);
            foreach (int nextNodeId in node.nextNodeIds)
            {
                selectableNodeIds.Add(nextNodeId);
            }

            return;
        }

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (node.stageIndex == FirstCombatStageIndex)
            {
                selectableNodeIds.Add(node.nodeId);
            }
        }
    }

    private static void DistributeAdditionalNodes(List<int> stageNodeCounts, HashSet<int> singleNodeStageIndices, int additionalNodeCount)
    {
        if (stageNodeCounts == null || stageNodeCounts.Count == 0 || additionalNodeCount <= 0)
        {
            return;
        }

        while (additionalNodeCount > 0)
        {
            List<int> candidateIndices = new List<int>();
            for (int stage = 0; stage < stageNodeCounts.Count; stage++)
            {
                if (singleNodeStageIndices.Contains(stage))
                {
                    continue;
                }

                if (stageNodeCounts[stage] < DefaultMaxNodesPerFloor)
                {
                    candidateIndices.Add(stage);
                }
            }

            if (candidateIndices.Count == 0)
            {
                Debug.LogWarning("[TrainingRunState] No more floors can receive additional nodes.");
                return;
            }

            int pickedIndex = Random.Range(0, candidateIndices.Count);
            stageNodeCounts[candidateIndices[pickedIndex]]++;
            additionalNodeCount--;
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
        }

        for (int nextIndex = 0; nextIndex < nextNodeCount; nextIndex++)
        {
            if (incomingCounts[nextIndex] <= 0)
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

    private static TrainingNodeType DetermineNodeType(int stage, int lane, int nodeCountInStage)
    {
        if (stage == StartStageIndex)
            return TrainingNodeType.Start;

        if (stage == BossStageIndex)
            return TrainingNodeType.Boss;

        int combatStageIndex = stage - FirstCombatStageIndex;
        if (combatStageIndex > 0 && combatStageIndex % 3 == 0 && lane == nodeCountInStage - 1)
            return TrainingNodeType.Rest;

        if (combatStageIndex > 0 && (combatStageIndex + lane) % 4 == 0)
            return TrainingNodeType.Named;

        return TrainingNodeType.Monster;
    }

    private static bool NodeRequiresBattle(TrainingNodeType nodeType)
    {
        return nodeType == TrainingNodeType.Monster
               || nodeType == TrainingNodeType.Named
               || nodeType == TrainingNodeType.Boss;
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
}
