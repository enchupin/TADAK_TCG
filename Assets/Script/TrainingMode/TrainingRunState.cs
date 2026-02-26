using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime-only state for a single training-mode run.
/// Persists while the game process is alive.
/// </summary>
public static class TrainingRunState
{
    private static readonly Dictionary<int, TrainingMapNodeData> nodesById = new Dictionary<int, TrainingMapNodeData>();
    private static readonly List<TrainingMapNodeData> orderedNodes = new List<TrainingMapNodeData>();
    private static readonly HashSet<int> selectableNodeIds = new HashSet<int>();
    private static readonly HashSet<int> clearedNodeIds = new HashSet<int>();

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

    public static void StartNewRun(string mapSceneName, string battleSceneName, int stageCount = 6, int laneCount = 3)
    {
        MapSceneName = mapSceneName;
        BattleSceneName = battleSceneName;

        StageCount = Mathf.Max(2, stageCount);
        LaneCount = Mathf.Max(2, laneCount);

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

        List<List<int>> stageNodeIds = new List<List<int>>();
        int nextId = 0;

        // Normal stages.
        for (int stage = 0; stage < stageCount - 1; stage++)
        {
            List<int> idsInStage = new List<int>();
            for (int lane = 0; lane < laneCount; lane++)
            {
                TrainingMapNodeData node = new TrainingMapNodeData
                {
                    nodeId = nextId++,
                    stageIndex = stage,
                    laneIndex = lane,
                    nodeType = DetermineNodeType(stage, lane, stageCount),
                    gridPosition = new Vector2(lane - (laneCount - 1) * 0.5f, stage)
                };

                nodesById[node.nodeId] = node;
                orderedNodes.Add(node);
                idsInStage.Add(node.nodeId);
            }
            stageNodeIds.Add(idsInStage);
        }

        // Final escape stage has one node.
        List<int> finalStageIds = new List<int>();
        TrainingMapNodeData escapeNode = new TrainingMapNodeData
        {
            nodeId = nextId++,
            stageIndex = stageCount - 1,
            laneIndex = laneCount / 2,
            nodeType = TrainingNodeType.Escape,
            gridPosition = new Vector2(0f, stageCount - 1)
        };
        nodesById[escapeNode.nodeId] = escapeNode;
        orderedNodes.Add(escapeNode);
        finalStageIds.Add(escapeNode.nodeId);
        stageNodeIds.Add(finalStageIds);

        // Connect stages.
        for (int stage = 0; stage < stageNodeIds.Count - 1; stage++)
        {
            List<int> currentStage = stageNodeIds[stage];
            List<int> nextStage = stageNodeIds[stage + 1];

            foreach (int currentNodeId in currentStage)
            {
                TrainingMapNodeData currentNode = nodesById[currentNodeId];

                if (nextStage.Count == 1)
                {
                    currentNode.nextNodeIds.Add(nextStage[0]);
                    continue;
                }

                foreach (int nextNodeId in nextStage)
                {
                    TrainingMapNodeData nextNode = nodesById[nextNodeId];
                    if (Mathf.Abs(nextNode.laneIndex - currentNode.laneIndex) <= 1)
                    {
                        currentNode.nextNodeIds.Add(nextNodeId);
                    }
                }
            }
        }
    }

    private static TrainingNodeType DetermineNodeType(int stage, int lane, int stageCount)
    {
        // Stage right before escape is always a named fight.
        if (stage == stageCount - 2)
            return TrainingNodeType.Named;

        // Periodic rest nodes.
        if (stage > 0 && stage % 3 == 0 && lane == LaneCount - 1)
            return TrainingNodeType.Rest;

        // Sprinkle named fights on some branches.
        if (stage > 0 && (stage + lane) % 4 == 0)
            return TrainingNodeType.Named;

        return TrainingNodeType.Monster;
    }
}
