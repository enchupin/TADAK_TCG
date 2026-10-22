using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only state for a single training-mode run.
/// Persists while the game process is alive.
/// </summary>
public static class TrainingRunState
{
    // v1.0 맵: 층 = stageIndex + 1, 층별 노드 수·탈출구·종류 비율·경로 보장은 rules.json training (R-50 ~ R-56)
    private const int StartStageIndex = 0;
    private const int FirstCombatStageIndex = 1;
    private const int MapBuildRetryWarningInterval = 100;
    // 노드 종류를 다시 뽑는 횟수 (넘으면 간선부터 다시 생성)
    private const int NodeTypeAssignAttempts = 200;
    // 이 층까지는 휴식 · 네임드 없이 일반 전투만 (사용자 결정 2026-10-22: 2 – 3층)
    private const int NoSpecialNodeUntilFloor = 3;
    // 조우 배정 탐색 상한 (넘으면 맵을 다시 생성)
    private const int EncounterAssignStepLimit = 20000;
    private static int encounterAssignSteps;
    private static int TotalStageCount => Mathf.Max(1, GameRules.Training.map.nodesPerFloor.Count);
    private static int FinalEscapeStageIndex => TotalStageCount - 1;

    private static readonly Dictionary<int, TrainingMapNodeData> nodesById = new Dictionary<int, TrainingMapNodeData>();
    private static readonly List<TrainingMapNodeData> orderedNodes = new List<TrainingMapNodeData>();
    private static readonly HashSet<int> selectableNodeIds = new HashSet<int>();
    private static readonly HashSet<int> clearedNodeIds = new HashSet<int>();
    private static readonly Dictionary<int, List<int>> validConnectionMasksByShape = new Dictionary<int, List<int>>();
    private static readonly Dictionary<Character, int> identityGaugeByCharacter = new Dictionary<Character, int>();

    public static bool IsRunActive { get; private set; }
    public static bool IsRunCompleted { get; private set; }
    public static bool IsRunFailed { get; private set; }

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

    // 훈련 동안 아이덴티티 충전 진행도는 전투가 끝나도 유지
    public static void SetIdentityGauges(IReadOnlyDictionary<Character, int> gauges)
    {
        identityGaugeByCharacter.Clear();
        if (gauges == null)
        {
            return;
        }

        foreach (KeyValuePair<Character, int> entry in gauges)
        {
            identityGaugeByCharacter[entry.Key] = Mathf.Max(0, entry.Value);
        }
    }

    public static IReadOnlyDictionary<Character, int> GetIdentityGauges()
    {
        return identityGaugeByCharacter;
    }

    public static void StartNewRun(string battleSceneName)
    {
        ResetRun();

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
        identityGaugeByCharacter.Clear();

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
            for (int lane = 0; lane < nodeCount; lane++)
            {
                TrainingNodeType nodeType = ResolveInitialNodeType(stage);
                TrainingMapNodeData node = new TrainingMapNodeData
                {
                    nodeId = nextId++,
                    stageIndex = stage,
                    laneIndex = lane,
                    nodeType = nodeType,
                    plannedEncounter = new List<int>()
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

        if (!TryAssignNodeTypes(stageNodeIds))
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

        // 8 · 15층 탈출구
        if (IsEscapeStage(stage))
            return TrainingNodeType.Event;

        return TrainingNodeType.Monster;
    }

    private static bool IsEscapeStage(int stageIndex)
    {
        return GameRules.Training.exitFloors.Contains(stageIndex + 1);
    }

    // R-51 2–7층 · 9–14층 노드는 휴식 / 일반 / 네임드 (시작 · 탈출구 제외)
    private static bool IsTypableStage(int stageIndex)
    {
        return stageIndex != StartStageIndex && !IsEscapeStage(stageIndex);
    }

    // 초반 층은 일반 전투만
    private static bool CanHoldSpecialNode(TrainingMapNodeData node)
    {
        return IsTypableStage(node.stageIndex) && node.FloorNumber > NoSpecialNodeUntilFloor;
    }

    // 휴식 연속 금지: 앞뒤로 연결된 노드에 휴식이 없어야 함
    private static bool CanBecomeRest(TrainingMapNodeData node)
    {
        if (!CanHoldSpecialNode(node))
        {
            return false;
        }

        foreach (int nextNodeId in node.nextNodeIds)
        {
            if (nodesById[nextNodeId].nodeType == TrainingNodeType.Rest)
            {
                return false;
            }
        }

        foreach (TrainingMapNodeData previous in orderedNodes)
        {
            if (previous.nodeType == TrainingNodeType.Rest && previous.nextNodeIds.Contains(node.nodeId))
            {
                return false;
            }
        }

        return true;
    }

    // type_weights 비율로 종류를 뽑은 뒤, R-55 경로 보장(1 → 15층 모든 경로에 휴식 2, 네임드 1 이상)이 부족한 경로의 일반 노드를 바꿔 채움
    // 비율만으로 다시 뽑으면 경로 수가 많아 거의 만족하지 못하므로 보정 방식 사용
    private static bool TryAssignNodeTypes(IReadOnlyList<List<int>> stageNodeIds)
    {
        for (int stage = 0; stage < stageNodeIds.Count; stage++)
        {
            if (!IsTypableStage(stage))
            {
                continue;
            }

            foreach (int nodeId in stageNodeIds[stage])
            {
                TrainingMapNodeData node = nodesById[nodeId];
                TrainingNodeType pickedType = CanHoldSpecialNode(node) ? PickWeightedNodeType() : TrainingNodeType.Monster;
                node.nodeType = pickedType == TrainingNodeType.Rest && !CanBecomeRest(node) ? TrainingNodeType.Monster : pickedType;
            }
        }

        TrainingMapNodeData.MapRules rules = TrainingMapNodeData.MapRules.Current;
        for (int attempt = 0; attempt < NodeTypeAssignAttempts; attempt++)
        {
            List<TrainingMapNodeData> weakestPath = FindWeakestFullPath(stageNodeIds, out int restCount, out int namedCount);
            bool needsRest = restCount < rules.minRestPerPath;
            bool needsNamed = namedCount < rules.minNamedPerPath;
            if (!needsRest && !needsNamed)
            {
                return true;
            }

            // 부족한 종류로 바꿀 일반 노드 (없으면 남는 쪽 특수 노드)
            List<TrainingMapNodeData> candidates = weakestPath.FindAll(node => node.nodeType == TrainingNodeType.Monster
                && (needsRest ? CanBecomeRest(node) : CanHoldSpecialNode(node)));
            if (candidates.Count == 0)
            {
                TrainingNodeType surplusType = needsRest ? TrainingNodeType.Named : TrainingNodeType.Rest;
                int surplusCount = needsRest ? namedCount - rules.minNamedPerPath : restCount - rules.minRestPerPath;
                if (surplusCount <= 0)
                {
                    return false;
                }

                candidates = needsRest
                    ? weakestPath.FindAll(node => node.nodeType == surplusType && CanBecomeRest(node))
                    : weakestPath.FindAll(node => node.nodeType == surplusType && CanHoldSpecialNode(node));
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates[UnityEngine.Random.Range(0, candidates.Count)].nodeType = needsRest ? TrainingNodeType.Rest : TrainingNodeType.Named;
        }

        return SatisfiesPathGuarantees(stageNodeIds);
    }

    // 시작 노드에서 마지막 층까지 휴식이 가장 적은 경로 (같으면 네임드가 적은 경로)
    private static List<TrainingMapNodeData> FindWeakestFullPath(IReadOnlyList<List<int>> stageNodeIds, out int restCount, out int namedCount)
    {
        Dictionary<int, (int rest, int named, int next)> bestByNodeId = new Dictionary<int, (int rest, int named, int next)>();
        for (int stage = stageNodeIds.Count - 1; stage >= 0; stage--)
        {
            foreach (int nodeId in stageNodeIds[stage])
            {
                TrainingMapNodeData node = nodesById[nodeId];
                (int rest, int named, int next) best = (0, 0, -1);
                if (node.nextNodeIds.Count > 0)
                {
                    best = (int.MaxValue, int.MaxValue, -1);
                    foreach (int nextNodeId in node.nextNodeIds)
                    {
                        (int rest, int named, int next) candidate = bestByNodeId[nextNodeId];
                        if (candidate.rest < best.rest || (candidate.rest == best.rest && candidate.named < best.named))
                        {
                            best = (candidate.rest, candidate.named, nextNodeId);
                        }
                    }
                }

                bestByNodeId[nodeId] = (best.rest + (node.nodeType == TrainingNodeType.Rest ? 1 : 0),
                    best.named + (node.nodeType == TrainingNodeType.Named ? 1 : 0), best.next);
            }
        }

        // 휴식이 충분하면 네임드가 가장 적은 경로를 다시 찾음
        int startNodeId = stageNodeIds[StartStageIndex][0];
        List<TrainingMapNodeData> path = TracePath(startNodeId, bestByNodeId);
        restCount = bestByNodeId[startNodeId].rest;
        namedCount = bestByNodeId[startNodeId].named;
        if (restCount >= TrainingMapNodeData.MapRules.Current.minRestPerPath)
        {
            Dictionary<int, (int rest, int named, int next)> namedByNodeId = new Dictionary<int, (int rest, int named, int next)>();
            for (int stage = stageNodeIds.Count - 1; stage >= 0; stage--)
            {
                foreach (int nodeId in stageNodeIds[stage])
                {
                    TrainingMapNodeData node = nodesById[nodeId];
                    (int rest, int named, int next) best = (0, 0, -1);
                    if (node.nextNodeIds.Count > 0)
                    {
                        best = (int.MaxValue, int.MaxValue, -1);
                        foreach (int nextNodeId in node.nextNodeIds)
                        {
                            (int rest, int named, int next) candidate = namedByNodeId[nextNodeId];
                            if (candidate.named < best.named)
                            {
                                best = (candidate.rest, candidate.named, nextNodeId);
                            }
                        }
                    }

                    namedByNodeId[nodeId] = (best.rest + (node.nodeType == TrainingNodeType.Rest ? 1 : 0),
                        best.named + (node.nodeType == TrainingNodeType.Named ? 1 : 0), best.next);
                }
            }

            if (namedByNodeId[startNodeId].named < TrainingMapNodeData.MapRules.Current.minNamedPerPath)
            {
                path = TracePath(startNodeId, namedByNodeId);
                restCount = namedByNodeId[startNodeId].rest;
                namedCount = namedByNodeId[startNodeId].named;
            }
            else
            {
                namedCount = namedByNodeId[startNodeId].named;
            }
        }

        return path;
    }

    private static List<TrainingMapNodeData> TracePath(int startNodeId, Dictionary<int, (int rest, int named, int next)> bestByNodeId)
    {
        List<TrainingMapNodeData> path = new List<TrainingMapNodeData>();
        int nodeId = startNodeId;
        while (nodeId >= 0)
        {
            path.Add(nodesById[nodeId]);
            nodeId = bestByNodeId[nodeId].next;
        }

        return path;
    }

    private static TrainingNodeType PickWeightedNodeType()
    {
        MapTypeWeightsData weights = GameRules.Training.map.typeWeights ?? new MapTypeWeightsData();
        float rest = Mathf.Max(0f, weights.rest);
        float normal = Mathf.Max(0f, weights.normal);
        float named = Mathf.Max(0f, weights.named);
        float roll = UnityEngine.Random.value * Mathf.Max(0.0001f, rest + normal + named);
        if (roll < rest)
        {
            return TrainingNodeType.Rest;
        }

        return roll < rest + normal ? TrainingNodeType.Monster : TrainingNodeType.Named;
    }

    // 마지막 층까지 이어지는 모든 경로 중 휴식 · 네임드가 가장 적은 경로 기준 (8층 탈출 경로는 보장하지 않음)
    public static bool SatisfiesPathGuarantees(IReadOnlyList<List<int>> stageNodeIds)
    {
        Dictionary<int, int> minRestByNodeId = new Dictionary<int, int>();
        Dictionary<int, int> minNamedByNodeId = new Dictionary<int, int>();
        for (int stage = stageNodeIds.Count - 1; stage >= 0; stage--)
        {
            foreach (int nodeId in stageNodeIds[stage])
            {
                TrainingMapNodeData node = nodesById[nodeId];
                int restAfter = 0;
                int namedAfter = 0;
                if (node.nextNodeIds.Count > 0)
                {
                    restAfter = int.MaxValue;
                    namedAfter = int.MaxValue;
                    foreach (int nextNodeId in node.nextNodeIds)
                    {
                        restAfter = Mathf.Min(restAfter, minRestByNodeId[nextNodeId]);
                        namedAfter = Mathf.Min(namedAfter, minNamedByNodeId[nextNodeId]);
                    }
                }

                minRestByNodeId[nodeId] = restAfter + (node.nodeType == TrainingNodeType.Rest ? 1 : 0);
                minNamedByNodeId[nodeId] = namedAfter + (node.nodeType == TrainingNodeType.Named ? 1 : 0);
            }
        }

        TrainingMapNodeData.MapRules rules = TrainingMapNodeData.MapRules.Current;
        foreach (int startNodeId in stageNodeIds[StartStageIndex])
        {
            if (minRestByNodeId[startNodeId] < rules.minRestPerPath || minNamedByNodeId[startNodeId] < rules.minNamedPerPath)
            {
                return false;
            }
        }

        return true;
    }

    private static List<int> CreatePlannedEncounter(TrainingNodeType nodeType, int stageIndex)
    {
        if (!TrainingNodeTypeUtility.RequiresBattle(nodeType))
        {
            return new List<int>();
        }

        // 조우 계층은 층 번호(stageIndex + 1) 기준
        return MonsterSpawner.CreateEncounterPlan(nodeType, stageIndex + 1);
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

            if (node.nodeType == TrainingNodeType.Monster || node.nodeType == TrainingNodeType.Named)
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

        // 한 경로에서 같은 계층 · 종류 노드가 그 조우 풀보다 많으면 배정할 수 없으므로 바로 다시 생성 (R-56)
        if (!HasEnoughEncountersOnEveryPath(candidatesByNodeId))
        {
            return false;
        }

        encounterAssignSteps = 0;
        return TryAssignMonsterEncountersRecursive(
            monsterNodes,
            conflictNodeIdsByNodeId,
            candidatesByNodeId,
            assignedSignatureByNodeId);
    }

    // 계층 · 종류별로, 어떤 경로든 그 묶음 노드 수가 후보 조우 수를 넘지 않는지 확인
    private static bool HasEnoughEncountersOnEveryPath(Dictionary<int, List<MonsterEncounterCandidate>> candidatesByNodeId)
    {
        Dictionary<string, int> poolSizeByGroup = new Dictionary<string, int>();
        foreach (KeyValuePair<int, List<MonsterEncounterCandidate>> entry in candidatesByNodeId)
        {
            poolSizeByGroup[BuildEncounterGroupKey(nodesById[entry.Key])] = entry.Value.Count;
        }

        foreach (KeyValuePair<string, int> group in poolSizeByGroup)
        {
            // 마지막 층부터 역순으로 경로상 같은 묶음 노드의 최대 수
            Dictionary<int, int> maxCountByNodeId = new Dictionary<int, int>();
            for (int i = orderedNodes.Count - 1; i >= 0; i--)
            {
                TrainingMapNodeData node = orderedNodes[i];
                int best = 0;
                foreach (int nextNodeId in node.nextNodeIds)
                {
                    best = Mathf.Max(best, maxCountByNodeId.TryGetValue(nextNodeId, out int nextCount) ? nextCount : 0);
                }

                bool inGroup = candidatesByNodeId.ContainsKey(node.nodeId) && BuildEncounterGroupKey(node) == group.Key;
                maxCountByNodeId[node.nodeId] = best + (inGroup ? 1 : 0);
                if (maxCountByNodeId[node.nodeId] > group.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static string BuildEncounterGroupKey(TrainingMapNodeData node)
    {
        return $"{node.nodeType}:{MonsterSpawner.ResolveEncounterTier(node.FloorNumber)}";
    }

    private static List<TrainingMapNodeData> CollectMonsterNodes()
    {
        List<TrainingMapNodeData> monsterNodes = new List<TrainingMapNodeData>();
        for (int i = 0; i < orderedNodes.Count; i++)
        {
            TrainingMapNodeData node = orderedNodes[i];
            // R-56 일반 · 네임드 조우 모두 한 경로에서 중복 금지
            if (node.nodeType != TrainingNodeType.Monster && node.nodeType != TrainingNodeType.Named)
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
            List<List<int>> rawCandidates = MonsterSpawner.GetEncounterCandidates(node.nodeType, node.FloorNumber);
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

        if (++encounterAssignSteps > EncounterAssignStepLimit)
        {
            return false;
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

            nextNode.plannedEncounter = new List<int>(candidate.encounter);
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
        public readonly List<int> encounter;
        public readonly string signature;

        public MonsterEncounterCandidate(List<int> encounter)
        {
            this.encounter = encounter;
            signature = MonsterSpawner.BuildEncounterSignature(encounter);
        }
    }

    private static List<int> BuildStageNodeCounts()
    {
        // R-50 층별 노드 수 (rules.json training.map.nodesPerFloor)
        List<int> stageNodeCounts = new List<int>(TotalStageCount);
        foreach (int count in GameRules.Training.map.nodesPerFloor)
        {
            stageNodeCounts.Add(Mathf.Max(1, count));
        }

        return stageNodeCounts;
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

        int maxOutDegree = Mathf.Max(1, GameRules.Training.map.maxOutDegree);
        for (int currentIndex = 0; currentIndex < currentNodeCount; currentIndex++)
        {
            if (outgoingCounts[currentIndex] <= 0 || outgoingCounts[currentIndex] > maxOutDegree)
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
