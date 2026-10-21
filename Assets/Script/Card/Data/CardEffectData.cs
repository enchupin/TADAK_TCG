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
    // 강공 효과 배율 (드래곤 브레스+ 등)
    public float boostMultiplier = 1f;
    // SelectCard 후보 카드 조건 (Disruption 등)
    public string cardFilter;

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
    public string characterFilter;
    public bool allowFewerSelection;
    public bool upgradeableOnly;
    // R-25 선택 후보에서 유일 카드 제외
    public bool excludeUnique;

    [Header("Keyword")]
    public string keyword;

    [Header("GenerateCard")]
    public string cardId;
    public List<RandomCardData> RandomCard;

    [Header("Control")]
    public string timing;
    public bool repeatNextEffect;
}
