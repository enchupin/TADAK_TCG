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
    private const int ForcedRestStageIndex = DefaultFloorCount - 1;
    private const int SingleNodeFloorNodeCount = 1;
    private const int MinNodesPerRegularFloor = 2;
    private const int MapBuildRetryWarningInterval = 100;
    private const int NoSpecialNodeUntilStageIndex = 3;
    private const int RequiredRestNodeCount = 9;
    private const int RequiredNamedNodeCount = 9;

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
        ResetRun();

        MapSceneName = mapSceneName;
        BattleSceneName = battleSceneName;

        BuildSimpleMap();

        InitializeRunStartState();
        IsRunActive = true;

        Debug.Log("[TrainingRunState] New run started.");
    }

    public static void ResetRun()
    {
        nodesById.Clear();
        orderedNodes.Clear();
        selectableNodeIds.Clear();
        clearedNodeIds.Clear();

        CurrentNodeId = null;
        PendingNodeId = null;

        HasPlayerHealthState = false;
        PlayerCurrentHp = 0;
        PlayerMaxHp = 0;

        MapSceneName = string.Empty;
        BattleSceneName = string.Empty;

        IsRunActive = false;
        IsRunCompleted = false;
        IsRunFailed = false;
    }

    private static void BuildSimpleMap()
    {
        int attemptCount = 0;
        while (true)
        {
            attemptCount++;
            if (TryBuildSimpleMapLayout())
            {
                if (attemptCount > 1)
                {
                    Debug.Log($"[TrainingRunState] 맵 배치를 {attemptCount}회 시도 후 완료했습니다");
                }

                return;
            }

            if (attemptCount % MapBuildRetryWarningInterval == 0)
            {
                Debug.LogWarning($"[TrainingRunState] 맵 배치 재시도가 {attemptCount}회 누적되었습니다");
            }
        }
    }

    private static bool TryBuildSimpleMapLayout()
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
                TrainingNodeType nodeType = ResolveInitialNodeType(stage);
                TrainingMapNodeData node = new TrainingMapNodeData
                {
                    nodeId = nextId++,
                    stageIndex = stage,
                    laneIndex = lane,
                    nodeType = nodeType,
                    gridPosition = new Vector2(stage, lane - centeredOffset),
                    plannedEncounter = new List<MonsterSpawner.SpawnMonsterType>()
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

        if (!TryAssignRestNodeTypes(stageNodeIds))
        {
            return false;
        }

        if (!TryAssignNamedNodeTypes())
        {
            return false;
        }

        AssignRandomEncountersToNonMonsterNodes();

        if (!TryAssignMonsterEncounters())
        {
            return false;
        }
        return true;
    }

    private static TrainingNodeType ResolveInitialNodeType(int stage)
    {
        if (stage == StartStageIndex)
            return TrainingNodeType.Start;

        if (stage == BossStageIndex)
            return TrainingNodeType.Boss;

        if (stage == MidTowerSingleNodeStageIndex)
            return TrainingNodeType.Event;

        return TrainingNodeType.Monster;
    }

    private static bool TryAssignRestNodeTypes(IReadOnlyList<List<int>> stageNodeIds)
    {
        if (stageNodeIds == null || stageNodeIds.Count <= ForcedRestStageIndex)
        {
            return false;
        }

        int forcedRestNodeCount = stageNodeIds[ForcedRestStageIndex].Count;
        int targetOptionalRestNodeCount = RequiredRestNodeCount - forcedRestNodeCount;
        if (targetOptionalRestNodeCount < 0)
        {
            return false;
        }

        if (GetMaximumRestNodeCountBeforeForcedStage(stageNodeIds, FirstCombatStageIndex) < targetOptionalRestNodeCount)
        {
            return false;
        }

        Dictionary<int, int> restMaskByStage = new Dictionary<int, int>();
        HashSet<int> failedStateKeys = new HashSet<int>();
        int startRestFreeMask = stageNodeIds[StartStageIndex].Count > 0 ? 1 : 0;

        if (!TryAssignRestNodeTypesRecursive(
                stageNodeIds,
                FirstCombatStageIndex,
                0,
                startRestFreeMask,
                0,
                targetOptionalRestNodeCount,
                restMaskByStage,
                failedStateKeys))
        {
            return false;
        }

        ApplyRestNodeTypes(stageNodeIds, restMaskByStage);
        return true;
    }

    private static bool TryAssignRestNodeTypesRecursive(
        IReadOnlyList<List<int>> stageNodeIds,
        int stageIndex,
        int previousRestMask,
        int previousRestFreeMask,
        int assignedRestNodeCount,
        int targetRestNodeCount,
        Dictionary<int, int> restMaskByStage,
        HashSet<int> failedStateKeys)
    {
        if (stageIndex >= ForcedRestStageIndex)
        {
            return previousRestFreeMask == 0 && assignedRestNodeCount == targetRestNodeCount;
        }

        int stateKey = BuildRestPlacementStateKey(stageIndex, previousRestMask, previousRestFreeMask, assignedRestNodeCount);
        if (failedStateKeys.Contains(stateKey))
        {
            return false;
        }

        List<int> candidateMasks = BuildRestCandidateMasks(stageNodeIds, stageIndex, previousRestMask);
        for (int i = 0; i < candidateMasks.Count; i++)
        {
            int currentRestMask = candidateMasks[i];
            int nextAssignedRestNodeCount = assignedRestNodeCount + CountMaskBits(currentRestMask);
            if (nextAssignedRestNodeCount > targetRestNodeCount)
            {
                continue;
            }

            if (nextAssignedRestNodeCount + GetMaximumRestNodeCountBeforeForcedStage(stageNodeIds, stageIndex + 1) < targetRestNodeCount)
            {
                continue;
            }

            int currentRestFreeMask = BuildRestFreeReachableMask(stageNodeIds, stageIndex, previousRestFreeMask, currentRestMask);

            restMaskByStage[stageIndex] = currentRestMask;
            if (TryAssignRestNodeTypesRecursive(
                    stageNodeIds,
                    stageIndex + 1,
                    currentRestMask,
                    currentRestFreeMask,
                    nextAssignedRestNodeCount,
                    targetRestNodeCount,
                    restMaskByStage,
                    failedStateKeys))
            {
                return true;
            }
        }

        restMaskByStage.Remove(stageIndex);
        failedStateKeys.Add(stateKey);
        return false;
    }

    private static int BuildRestPlacementStateKey(int stageIndex, int previousRestMask, int previousRestFreeMask, int assignedRestNodeCount)
    {
        return stageIndex | (previousRestMask << 8) | (previousRestFreeMask << 16) | (assignedRestNodeCount << 24);
    }

    private static int GetMaximumRestNodeCountBeforeForcedStage(IReadOnlyList<List<int>> stageNodeIds, int startStageIndex)
    {
        int maxRestNodeCount = 0;
        int clampedStartStageIndex = Mathf.Max(startStageIndex, FirstCombatStageIndex);
        int lastOptionalRestStageIndex = Mathf.Min(ForcedRestStageIndex - 1, stageNodeIds.Count - 1);

        for (int stageIndex = clampedStartStageIndex; stageIndex <= lastOptionalRestStageIndex; stageIndex++)
        {
            int nodeCount = stageNodeIds[stageIndex].Count;
            if (TryGetFixedRestMask(stageIndex, nodeCount, out int fixedRestMask))
            {
                maxRestNodeCount += CountMaskBits(fixedRestMask);
                continue;
            }

            maxRestNodeCount += nodeCount;
        }

        return maxRestNodeCount;
    }

    private static int CountMaskBits(int mask)
    {
        int bitCount = 0;
        while (mask != 0)
        {
            bitCount += mask & 1;
            mask >>= 1;
        }

        return bitCount;
    }

    private static List<int> BuildRestCandidateMasks(IReadOnlyList<List<int>> stageNodeIds, int stageIndex, int previousRestMask)
    {
        int nodeCount = stageNodeIds[stageIndex].Count;
        List<int> candidateMasks = new List<int>();

        if (TryGetFixedRestMask(stageIndex, nodeCount, out int fixedRestMask))
        {
            if (IsRestMaskCompatibleWithPreviousStage(stageNodeIds, stageIndex, previousRestMask, fixedRestMask))
            {
                candidateMasks.Add(fixedRestMask);
            }

            return candidateMasks;
        }

        int maxMask = 1 << nodeCount;
        for (int mask = 0; mask < maxMask; mask++)
        {
            if (IsRestMaskCompatibleWithPreviousStage(stageNodeIds, stageIndex, previousRestMask, mask))
            {
                candidateMasks.Add(mask);
            }
        }

        ShuffleList(candidateMasks);
        return candidateMasks;
    }

    private static bool TryGetFixedRestMask(int stageIndex, int nodeCount, out int fixedRestMask)
    {
        fixedRestMask = 0;

        if (stageIndex <= NoSpecialNodeUntilStageIndex
            || stageIndex == MidTowerSingleNodeStageIndex
            || stageIndex == ForcedRestStageIndex - 1)
        {
            return true;
        }

        if (stageIndex == ForcedRestStageIndex)
        {
            fixedRestMask = (1 << nodeCount) - 1;
            return true;
        }

        return false;
    }

    private static bool IsRestMaskCompatibleWithPreviousStage(
        IReadOnlyList<List<int>> stageNodeIds,
        int stageIndex,
        int previousRestMask,
        int currentRestMask)
    {
        if (previousRestMask == 0 || currentRestMask == 0)
        {
            return true;
        }

        List<int> previousStageNodeIds = stageNodeIds[stageIndex - 1];
        List<int> currentStageNodeIds = stageNodeIds[stageIndex];

        for (int currentIndex = 0; currentIndex < currentStageNodeIds.Count; currentIndex++)
        {
            if ((currentRestMask & (1 << currentIndex)) == 0)
            {
                continue;
            }

            int currentNodeId = currentStageNodeIds[currentIndex];
            for (int previousIndex = 0; previousIndex < previousStageNodeIds.Count; previousIndex++)
            {
                if ((previousRestMask & (1 << previousIndex)) == 0)
                {
                    continue;
                }

                if (nodesById.TryGetValue(previousStageNodeIds[previousIndex], out TrainingMapNodeData previousNode)
                    && previousNode.nextNodeIds.Contains(currentNodeId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static int BuildRestFreeReachableMask(
        IReadOnlyList<List<int>> stageNodeIds,
        int stageIndex,
        int previousRestFreeMask,
        int currentRestMask)
    {
        int currentRestFreeMask = 0;
        List<int> previousStageNodeIds = stageNodeIds[stageIndex - 1];
        List<int> currentStageNodeIds = stageNodeIds[stageIndex];

        for (int currentIndex = 0; currentIndex < currentStageNodeIds.Count; currentIndex++)
        {
            if ((currentRestMask & (1 << currentIndex)) != 0)
            {
                continue;
            }

            int currentNodeId = currentStageNodeIds[currentIndex];
            for (int previousIndex = 0; previousIndex < previousStageNodeIds.Count; previousIndex++)
            {
                if ((previousRestFreeMask & (1 << previousIndex)) == 0)
                {
                    continue;
                }

                if (nodesById.TryGetValue(previousStageNodeIds[previousIndex], out TrainingMapNodeData previousNode)
                    && previousNode.nextNodeIds.Contains(currentNodeId))
                {
                    currentRestFreeMask |= 1 << currentIndex;
                    break;
                }
            }
        }

        return currentRestFreeMask;
    }

    private static void ApplyRestNodeTypes(IReadOnlyList<List<int>> stageNodeIds, IReadOnlyDictionary<int, int> restMaskByStage)
    {
        if (stageNodeIds.Count > ForcedRestStageIndex)
        {
            List<int> forcedRestNodeIds = stageNodeIds[ForcedRestStageIndex];
            for (int i = 0; i < forcedRestNodeIds.Count; i++)
            {
                if (nodesById.TryGetValue(forcedRestNodeIds[i], out TrainingMapNodeData forcedRestNode))
                {
                    forcedRestNode.nodeType = TrainingNodeType.Rest;
                }
            }
        }

        foreach (KeyValuePair<int, int> restMaskEntry in restMaskByStage)
        {
            List<int> stageNodeIdList = stageNodeIds[restMaskEntry.Key];
            for (int nodeIndex = 0; nodeIndex < stageNodeIdList.Count; nodeIndex++)
            {
                if ((restMaskEntry.Value & (1 << nodeIndex)) == 0)
                {
                    continue;
                }

                if (nodesById.TryGetValue(stageNodeIdList[nodeIndex], out TrainingMapNodeData restNode))
                {
                    restNode.nodeType = TrainingNodeType.Rest;
                }
            }
        }
    }

    private static bool TryAssignNamedNodeTypes()
    {
        Dictionary<int, HashSet<int>> directConflictNodeIdsByNodeId = BuildDirectConflictNodeIdsByNodeId();
        HashSet<int> blockedNodeIds = new HashSet<int>();

        for (int assignedCount = 0; assignedCount < RequiredNamedNodeCount; assignedCount++)
        {
            List<TrainingMapNodeData> candidates = CollectAvailableNamedNodeCandidates(blockedNodeIds);
            if (candidates.Count == 0)
            {
                return false;
            }

            int pickedIndex = UnityEngine.Random.Range(0, candidates.Count);
            TrainingMapNodeData selectedNode = candidates[pickedIndex];
            selectedNode.nodeType = TrainingNodeType.Named;

            blockedNodeIds.Add(selectedNode.nodeId);
            if (directConflictNodeIdsByNodeId.TryGetValue(selectedNode.nodeId, out HashSet<int> conflictNodeIds))
            {
                foreach (int conflictNodeId in conflictNodeIds)
                {
                    blockedNodeIds.Add(conflictNodeId);
                }
            }
        }

        return true;
    }

    private static Dictionary<int, HashSet<int>> BuildDirectConflictNodeIdsByNodeId()
    {
        Dictionary<int, HashSet<int>> directConflictNodeIdsByNodeId = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            directConflictNodeIdsByNodeId[orderedNodes[i].nodeId] = new HashSet<int>();
        }

        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (!directConflictNodeIdsByNodeId.TryGetValue(node.nodeId, out HashSet<int> currentNodeConflictIds))
            {
                continue;
            }

            for (int nextIndex = 0; nextIndex < node.nextNodeIds.Count; nextIndex++)
            {
                int nextNodeId = node.nextNodeIds[nextIndex];
                currentNodeConflictIds.Add(nextNodeId);
                if (directConflictNodeIdsByNodeId.TryGetValue(nextNodeId, out HashSet<int> nextNodeConflictIds))
                {
                    nextNodeConflictIds.Add(node.nodeId);
                }
            }
        }

        return directConflictNodeIdsByNodeId;
    }

    private static List<TrainingMapNodeData> CollectAvailableNamedNodeCandidates(HashSet<int> blockedNodeIds)
    {
        List<TrainingMapNodeData> candidates = new List<TrainingMapNodeData>();
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (node.stageIndex <= NoSpecialNodeUntilStageIndex
                || node.nodeType != TrainingNodeType.Monster
                || blockedNodeIds.Contains(node.nodeId))
            {
                continue;
            }

            candidates.Add(node);
        }

        return candidates;
    }

    private static List<MonsterSpawner.SpawnMonsterType> CreatePlannedEncounter(TrainingNodeType nodeType, int stageIndex)
    {
        if (!TrainingNodeTypeUtility.RequiresBattle(nodeType))
        {
            return new List<MonsterSpawner.SpawnMonsterType>();
        }

        return MonsterSpawner.CreateEncounterPlan(nodeType, stageIndex);
    }

    private static void AssignRandomEncountersToNonMonsterNodes()
    {
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (!TrainingNodeTypeUtility.RequiresBattle(node.nodeType))
            {
                node.plannedEncounter.Clear();
                continue;
            }

            if (node.nodeType == TrainingNodeType.Monster)
            {
                node.plannedEncounter.Clear();
                continue;
            }

            node.plannedEncounter = CreatePlannedEncounter(node.nodeType, node.stageIndex);
        }
    }

    private static bool TryAssignMonsterEncounters()
    {
        List<TrainingMapNodeData> monsterNodes = CollectMonsterNodes();
        if (monsterNodes.Count == 0)
        {
            return true;
        }

        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId = BuildMonsterConflictNodeIdsByNodeId(monsterNodes);
        Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId = BuildMonsterCandidatesByNodeId(monsterNodes);
        Dictionary<int, string> assignedSignatureByNodeId = new Dictionary<int, string>();

        return TryAssignMonsterEncountersRecursive(
            monsterNodes,
            conflictNodeIdsByNodeId,
            candidatesByNodeId,
            assignedSignatureByNodeId);
    }

    private static List<TrainingMapNodeData> CollectMonsterNodes()
    {
        List<TrainingMapNodeData> monsterNodes = new List<TrainingMapNodeData>();
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            if (node.nodeType != TrainingNodeType.Monster)
            {
                continue;
            }

            monsterNodes.Add(node);
        }

        return monsterNodes;
    }

    private static Dictionary<int, List<MonsterEncounterCandidate>> BuildMonsterCandidatesByNodeId(IReadOnlyList<TrainingMapNodeData> monsterNodes)
    {
        Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId = new Dictionary<int, List<MonsterEncounterCandidate>>();
        for (int i = 0; i < monsterNodes.Count; i++)
        {
            TrainingMapNodeData node = monsterNodes[i];
            List<List<MonsterSpawner.SpawnMonsterType>> rawCandidates = MonsterSpawner.GetEncounterCandidates(node.nodeType, node.stageIndex);
            ShuffleList(rawCandidates);

            HashSet<string> addedSignatures = new HashSet<string>();
            List<MonsterEncounterCandidate> candidates = new List<MonsterEncounterCandidate>();
            for (int candidateIndex = 0; candidateIndex < rawCandidates.Count; candidateIndex++)
            {
                MonsterEncounterCandidate candidate = new MonsterEncounterCandidate(rawCandidates[candidateIndex]);
                if (!addedSignatures.Add(candidate.signature))
                {
                    continue;
                }

                candidates.Add(candidate);
            }

            candidatesByNodeId[node.nodeId] = candidates;
        }

        return candidatesByNodeId;
    }

    private static Dictionary<int, HashSet<int>> BuildMonsterConflictNodeIdsByNodeId(IReadOnlyList<TrainingMapNodeData> monsterNodes)
    {
        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId = new Dictionary<int, HashSet<int>>();
        HashSet<int> monsterNodeIds = new HashSet<int>();

        for (int i = 0; i < monsterNodes.Count; i++)
        {
            int nodeId = monsterNodes[i].nodeId;
            conflictNodeIdsByNodeId[nodeId] = new HashSet<int>();
            monsterNodeIds.Add(nodeId);
        }

        for (int i = 0; i < monsterNodes.Count; i++)
        {
            TrainingMapNodeData node = monsterNodes[i];
            HashSet<int> reachableMonsterNodeIds = FindReachableMonsterNodeIds(node, monsterNodeIds);
            foreach (int reachableMonsterNodeId in reachableMonsterNodeIds)
            {
                conflictNodeIdsByNodeId[node.nodeId].Add(reachableMonsterNodeId);
                conflictNodeIdsByNodeId[reachableMonsterNodeId].Add(node.nodeId);
            }
        }

        return conflictNodeIdsByNodeId;
    }

    private static HashSet<int> FindReachableMonsterNodeIds(TrainingMapNodeData startNode, HashSet<int> monsterNodeIds)
    {
        HashSet<int> reachableMonsterNodeIds = new HashSet<int>();
        HashSet<int> visitedNodeIds = new HashSet<int>();
        Stack<int> pendingNodeIds = new Stack<int>();

        for (int i = 0; i < startNode.nextNodeIds.Count; i++)
        {
            pendingNodeIds.Push(startNode.nextNodeIds[i]);
        }

        while (pendingNodeIds.Count > 0)
        {
            int nodeId = pendingNodeIds.Pop();
            if (!visitedNodeIds.Add(nodeId))
            {
                continue;
            }

            if (!nodesById.TryGetValue(nodeId, out TrainingMapNodeData node))
            {
                continue;
            }

            if (monsterNodeIds.Contains(nodeId))
            {
                reachableMonsterNodeIds.Add(nodeId);
            }

            for (int i = 0; i < node.nextNodeIds.Count; i++)
            {
                pendingNodeIds.Push(node.nextNodeIds[i]);
            }
        }

        return reachableMonsterNodeIds;
    }

    private static bool TryAssignMonsterEncountersRecursive(
        IReadOnlyList<TrainingMapNodeData> monsterNodes,
        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId,
        Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId,
        Dictionary<int, string> assignedSignatureByNodeId)
    {
        if (assignedSignatureByNodeId.Count >= monsterNodes.Count)
        {
            return true;
        }

        TrainingMapNodeData nextNode = SelectNextMonsterAssignmentNode(
            monsterNodes,
            conflictNodeIdsByNodeId,
            candidatesByNodeId,
            assignedSignatureByNodeId,
            out int availableCandidateCount);

        if (nextNode == null || availableCandidateCount <= 0)
        {
            return false;
        }

        List<MonsterEncounterCandidate> candidates = candidatesByNodeId[nextNode.nodeId];
        for (int i = 0; i < candidates.Count; i++)
        {
            MonsterEncounterCandidate candidate = candidates[i];
            if (!CanAssignMonsterEncounterSignature(nextNode.nodeId, candidate.signature, conflictNodeIdsByNodeId, assignedSignatureByNodeId))
            {
                continue;
            }

            nextNode.plannedEncounter = new List<MonsterSpawner.SpawnMonsterType>(candidate.encounter);
            assignedSignatureByNodeId[nextNode.nodeId] = candidate.signature;

            if (TryAssignMonsterEncountersRecursive(
                monsterNodes,
                conflictNodeIdsByNodeId,
                candidatesByNodeId,
                assignedSignatureByNodeId))
            {
                return true;
            }

            assignedSignatureByNodeId.Remove(nextNode.nodeId);
            nextNode.plannedEncounter.Clear();
        }

        return false;
    }

    private static TrainingMapNodeData SelectNextMonsterAssignmentNode(
        IReadOnlyList<TrainingMapNodeData> monsterNodes,
        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId,
        Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId,
        Dictionary<int, string> assignedSignatureByNodeId,
        out int availableCandidateCount)
    {
        TrainingMapNodeData selectedNode = null;
        availableCandidateCount = int.MaxValue;
        int selectedConflictCount = -1;

        for (int i = 0; i < monsterNodes.Count; i++)
        {
            TrainingMapNodeData node = monsterNodes[i];
            if (assignedSignatureByNodeId.ContainsKey(node.nodeId))
            {
                continue;
            }

            int currentAvailableCandidateCount = CountAvailableMonsterCandidates(
                node.nodeId,
                conflictNodeIdsByNodeId,
                candidatesByNodeId,
                assignedSignatureByNodeId);
            int conflictCount = conflictNodeIdsByNodeId[node.nodeId].Count;

            if (selectedNode == null
                || currentAvailableCandidateCount < availableCandidateCount
                || (currentAvailableCandidateCount == availableCandidateCount && conflictCount > selectedConflictCount))
            {
                selectedNode = node;
                availableCandidateCount = currentAvailableCandidateCount;
                selectedConflictCount = conflictCount;
            }
        }

        return selectedNode;
    }

    private static int CountAvailableMonsterCandidates(
        int nodeId,
        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId,
        Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId,
        Dictionary<int, string> assignedSignatureByNodeId)
    {
        int availableCount = 0;
        List<MonsterEncounterCandidate> candidates = candidatesByNodeId[nodeId];
        for (int i = 0; i < candidates.Count; i++)
        {
            if (CanAssignMonsterEncounterSignature(nodeId, candidates[i].signature, conflictNodeIdsByNodeId, assignedSignatureByNodeId))
            {
                availableCount++;
            }
        }

        return availableCount;
    }

    private static bool CanAssignMonsterEncounterSignature(
        int nodeId,
        string encounterSignature,
        Dictionary<int, HashSet<int>> conflictNodeIdsByNodeId,
        Dictionary<int, string> assignedSignatureByNodeId)
    {
        HashSet<int> conflictNodeIds = conflictNodeIdsByNodeId[nodeId];
        foreach (int conflictNodeId in conflictNodeIds)
        {
            if (assignedSignatureByNodeId.TryGetValue(conflictNodeId, out string assignedSignature)
                && assignedSignature == encounterSignature)
            {
                return false;
            }
        }

        return true;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
        {
            return;
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private sealed class MonsterEncounterCandidate
    {
        public readonly List<MonsterSpawner.SpawnMonsterType> encounter;
        public readonly string signature;

        public MonsterEncounterCandidate(List<MonsterSpawner.SpawnMonsterType> encounter)
        {
            this.encounter = encounter;
            signature = MonsterSpawner.BuildEncounterSignature(encounter);
        }
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

            int pickedIndex = UnityEngine.Random.Range(0, candidateIndices.Count);
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
            ? validMasks[UnityEngine.Random.Range(0, validMasks.Count)]
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
        int maxConnectionCount = GetMaxConnectionCount(currentNodeCount, nextNodeCount);
        if (CountMaskBits(mask) > maxConnectionCount)
        {
            return false;
        }

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

    private static int GetMaxConnectionCount(int currentNodeCount, int nextNodeCount)
    {
        switch (currentNodeCount)
        {
            case 2:
                switch (nextNodeCount)
                {
                    case 2:
                        return 3;
                    case 3:
                        return 3;
                    case 4:
                        return 4;
                }
                break;
            case 3:
                switch (nextNodeCount)
                {
                    case 2:
                        return 4;
                    case 3:
                        return 4;
                    case 4:
                        return 5;
                }
                break;
            case 4:
                switch (nextNodeCount)
                {
                    case 2:
                        return 5;
                    case 3:
                        return 6;
                    case 4:
                        return 6;
                }
                break;
        }

        return currentNodeCount * nextNodeCount;
    }

    private static bool HasConnection(int mask, int currentIndex, int nextIndex, int nextNodeCount)
    {
        int bitIndex = currentIndex * nextNodeCount + nextIndex;
        return (mask & (1 << bitIndex)) != 0;
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
