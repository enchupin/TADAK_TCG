using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainingNodeType
{
    Monster,
    Named,
    Rest
}

[Serializable]
public class TrainingMapNodeData
{
    public int nodeId;
    public int stageIndex;
    public int laneIndex;
    public TrainingNodeType nodeType;
    public Vector2 gridPosition;
    public List<int> nextNodeIds = new List<int>();

    public string GetShortLabel()
    {
        switch (nodeType)
        {
            case TrainingNodeType.Monster:
                return "M";
            case TrainingNodeType.Named:
                return "N";
            case TrainingNodeType.Rest:
                return "R";
            default:
                return "M";
        }
    }
}
