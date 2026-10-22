using System;
using System.Collections.Generic;

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
    // v1.0 몬스터 ID(4자리) 목록 (encounters.json 조우 구성)
    public List<int> plannedEncounter = new List<int>();
    public List<int> nextNodeIds = new List<int>();

    public bool HasPlannedEncounter => plannedEncounter != null && plannedEncounter.Count > 0;
    // 층 번호 (1층 시작 · 8 · 15층 탈출구)
    public int FloorNumber => stageIndex + 1;

    // 경로 보장 수치 (rules.json training.map)
    public readonly struct MapRules
    {
        public readonly int minRestPerPath;
        public readonly int minNamedPerPath;

        private MapRules(int minRestPerPath, int minNamedPerPath)
        {
            this.minRestPerPath = minRestPerPath;
            this.minNamedPerPath = minNamedPerPath;
        }

        public static MapRules Current => new MapRules(GameRules.Training.map.minRestPerPath, GameRules.Training.map.minNamedPerPath);
    }
}
