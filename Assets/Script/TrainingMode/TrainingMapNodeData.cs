using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainingNodeType
{
    Start,
    Monster,
    Named,
    Rest,
    Boss
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
}
