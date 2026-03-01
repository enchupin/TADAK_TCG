using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ConditionData
{
    public List<CheckData> checks = new();
    [SerializeReference] public List<CardEffectData> successEffects = new();
    [SerializeReference] public List<CardEffectData> elseEffects = new();
}
