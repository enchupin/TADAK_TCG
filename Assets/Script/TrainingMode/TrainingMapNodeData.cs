using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainingNodeType
{
    Start,
    Monster,
    Named,
    Rest,
    Event,
    Boss
}

public static class TrainingNodeTypeUtility
{
    public static bool RequiresBattle(TrainingNodeType nodeType)
    {
        return nodeType == TrainingNodeType.Monster
               || nodeType == TrainingNodeType.Named
               || nodeType == TrainingNodeType.Boss;
    }

    public static bool TryGetEncounterLabelPrefix(TrainingNodeType nodeType, out string prefix)
    {
        switch (nodeType)
        {
            case TrainingNodeType.Monster:
                prefix = "M";
                return true;
            case TrainingNodeType.Named:
                prefix = "N";
                return true;
            case TrainingNodeType.Boss:
                prefix = "B";
                return true;
            default:
                prefix = string.Empty;
                return false;
        }
    }
}

[Serializable]
public class TrainingMapNodeData
{
    public int nodeId;
    public int stageIndex;
    public int laneIndex;
    public TrainingNodeType nodeType;
    public Vector2 gridPosition;
    public List<MonsterSpawner.SpawnMonsterType> plannedEncounter = new List<MonsterSpawner.SpawnMonsterType>();
    public List<int> nextNodeIds = new List<int>();

    public bool HasPlannedEncounter => plannedEncounter != null && plannedEncounter.Count > 0;
}
