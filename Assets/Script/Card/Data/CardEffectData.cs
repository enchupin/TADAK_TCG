using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Serializable card effect data.
/// </summary>
[System.Serializable]
public class CardEffectData
{
    public EffectType type;

    [Header("Common")]
    public int amount;
    public string amountFormula;
    public TargetType target;

    [Header("Buff")]
    public string stat;
    public int buffId;
    public int duration;

    [Header("DamagePerCardPlayed")]
    public int baseDamage;
    public int bonusPerCard;

    [Header("Execute")]
    public float hpThreshold;
    public float multiplier;

    [Header("Nested / Conditional")]
    [SerializeReference] public List<CardEffectData> subEffects;

    [Header("Conditional (Structured)")]
    public ConditionData conditionData;

    [Header("Reactive Effect")]
    [SerializeReference] public List<CardEffectData> onAction;
    public string subject;

    [Header("Targeting / Selection")]
    public int count;

    [Header("Keyword")]
    public string keyword;

    [Header("GenerateCard")]
    public List<RandomCardData> RandomCard;
}
