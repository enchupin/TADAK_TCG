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
    public string source;
    public string param;
    public string operation;
    public string upgrade;
    public string durationText;
    public int effectIndex;
    public float ampMultiplier = 1f;

    [Header("Buff")]
    public string stat;
    public int buffId;
    public int duration;
    public List<int> buffFilterIds;
    public bool random;
    public string change;

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

    [Header("Move")]
    public MoveZoneType from;
    public MoveZoneType to;
    public MovePositionType position;

    [Header("Targeting / Selection")]
    public int count;
    public List<int> formulaCardIdFilter;

    [Header("Keyword")]
    public string keyword;

    [Header("GenerateCard")]
    public string cardId;
    public List<RandomCardData> RandomCard;

    [Header("Control")]
    public string timing;
    public bool repeatNextEffect;
}
