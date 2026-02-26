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
    [SerializeReference] public CardEffectData nestedEffect;
    [SerializeReference] public List<CardEffectData> subEffects;
    [SerializeReference] public CardEffectData failEffect;

    [Header("Conditional (Structured)")]
    public ConditionData conditionData;

    // Legacy fields
    public string condition;
    public string conditionValue;

    [Header("Reactive Effect")]
    [SerializeReference] public CardEffectData onAction;

    [Header("Targeting / Selection")]
    public List<int> cardIdList;
    public int count;

    [Header("Keyword")]
    public string keyword;
    public int keywordId;

    [Header("GenerateCard")]
    public List<RandomCardData> RandomCard;

    /// <summary>
    /// Converts CardEffectData to runtime ICardEffect.
    /// </summary>
    public ICardEffect CreateEffect()
    {
        switch (type)
        {
            case EffectType.Repeat:
                List<ICardEffect> repeatedEffects = new List<ICardEffect>();
                if (subEffects != null)
                {
                    foreach (CardEffectData sub in subEffects)
                    {
                        ICardEffect eff = sub.CreateEffect();
                        if (eff != null) repeatedEffects.Add(eff);
                    }
                }
                return new RepeatEffect { count = count, countFormula = amountFormula, effectsToRepeat = repeatedEffects };

            case EffectType.Conditional:
                List<ICardEffect> success = BuildConditionalSuccessEffects();
                List<ICardEffect> fail = BuildConditionalFailEffects();
                return new ConditionalEffect
                {
                    conditionData = conditionData,
                    condition = condition,
                    conditionValue = conditionValue,
                    successEffects = success,
                    failEffects = fail
                };

            case EffectType.Attack:
                return new AttackEffect { amount = amount, amountFormula = amountFormula, cardIdList = cardIdList, target = target, onAction = onAction?.CreateEffect() };
            case EffectType.Barrier:
                return new BarrierEffect { amount = amount, amountFormula = amountFormula, target = target, onAction = onAction?.CreateEffect() };
            case EffectType.DiscardHand:
                return new DiscardHandEffect { count = count, amountFormula = amountFormula, target = target };
            case EffectType.ExhaustHand:
                return new ExhaustHandEffect { amountFormula = amountFormula };
            case EffectType.Scry:
                return new ScryEffect { count = count };
            case EffectType.ChoiceHand:
                return new ChoiceHandEffect { count = count };
            case EffectType.SelectCard:
                return new SelectCardEffect { count = count, target = target };
            case EffectType.Damage:
                return new DamageEffect { amount = amount, amountFormula = amountFormula, target = target, onAction = onAction?.CreateEffect() };
            case EffectType.Draw:
                return new DrawEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.Buff:
                return new BuffEffect { stat = stat, buffId = buffId, amount = amount, amountFormula = amountFormula, duration = duration, target = target };
            case EffectType.Energy:
                return new EnergyEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.DamagePerCardPlayed:
                return new DamagePerCardPlayedEffect { baseDamage = baseDamage, bonusPerCard = bonusPerCard };
            case EffectType.Execute:
                return new ExecuteDamageEffect { baseDamage = baseDamage, hpThreshold = hpThreshold, multiplier = multiplier };
            case EffectType.Heal:
                return new HealEffect { amount = amount, amountFormula = amountFormula, target = target };
            case EffectType.MultiplyDefense:
                return new MultiplyDefenseEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.ConsumeDefense:
                return new ConsumeDefenseEffect { amount = amount, amountFormula = amountFormula, nestedEffect = nestedEffect?.CreateEffect() };
            case EffectType.GenerateCard:
                return new GenerateCardEffect { RandomCard = RandomCard, cardIdList = cardIdList, target = target };
            case EffectType.Keyword:
                return new KeywordEffect { keyword = keyword, amount = amount, amountFormula = amountFormula };
            case EffectType.ChoiceDiscard:
                return new ChoiceDiscardEffect { amount = count, effect = nestedEffect?.CreateEffect() };
            case EffectType.Pickup:
                return new PickupEffect();
            case EffectType.RandomGenerate:
                Debug.LogWarning($"[CardData] Not implemented effect type: {type}");
                return null;
            default:
                Debug.LogWarning($"[CardData] Unknown effect type: {type}");
                return null;
        }
    }

    private List<ICardEffect> BuildConditionalSuccessEffects()
    {
        List<ICardEffect> success = new List<ICardEffect>();

        if (conditionData != null)
        {
            if (conditionData.successEffects != null && conditionData.successEffects.Count > 0)
            {
                foreach (CardEffectData effectData in conditionData.successEffects)
                {
                    ICardEffect effect = effectData?.CreateEffect();
                    if (effect != null) success.Add(effect);
                }

                return success;
            }

            if (conditionData.successEffect != null)
            {
                ICardEffect effect = conditionData.successEffect.CreateEffect();
                if (effect != null) success.Add(effect);
                return success;
            }
        }

        if (subEffects != null)
        {
            foreach (CardEffectData effectData in subEffects)
            {
                ICardEffect effect = effectData?.CreateEffect();
                if (effect != null) success.Add(effect);
            }
        }

        return success;
    }

    private List<ICardEffect> BuildConditionalFailEffects()
    {
        List<ICardEffect> fail = new List<ICardEffect>();

        if (conditionData != null)
        {
            if (conditionData.elseEffects != null && conditionData.elseEffects.Count > 0)
            {
                foreach (CardEffectData effectData in conditionData.elseEffects)
                {
                    ICardEffect effect = effectData?.CreateEffect();
                    if (effect != null) fail.Add(effect);
                }

                return fail;
            }

            if (conditionData.failEffect != null)
            {
                ICardEffect effect = conditionData.failEffect.CreateEffect();
                if (effect != null) fail.Add(effect);
                return fail;
            }
        }

        if (failEffect != null)
        {
            ICardEffect effect = failEffect.CreateEffect();
            if (effect != null) fail.Add(effect);
        }

        return fail;
    }
}

/// <summary>
/// Condition data (And/Or group).
/// </summary>
[System.Serializable]
public class ConditionData
{
    public string mode = "And";
    public List<CheckData> checks = new List<CheckData>();

    // Single-effect legacy fields
    [SerializeReference] public CardEffectData successEffect;
    [SerializeReference] public CardEffectData failEffect;

    // Multi-effect fields for JSON: effects / elseEffects
    [SerializeReference] public List<CardEffectData> successEffects = new List<CardEffectData>();
    [SerializeReference] public List<CardEffectData> elseEffects = new List<CardEffectData>();
}

/// <summary>
/// Single condition check.
/// </summary>
[System.Serializable]
public class CheckData
{
    public string subject;
    public string property;
    public string param;
    public string @operator;
    public string value;
}

/// <summary>
/// Effect type enum.
/// </summary>
public enum EffectType
{
    Damage,
    Draw,
    Buff,
    Energy,
    DamagePerCardPlayed,
    Execute,
    Heal,
    MultiplyDefense,
    ConsumeDefense,
    GenerateCard,
    Keyword,
    Attack,
    Barrier,
    ChoiceHand,
    ChoiceDiscard,
    Conditional,
    RandomGenerate,
    DiscardHand,
    ExhaustHand,
    SelectCard,
    Copy,
    Repeat,
    Scry,
    RemoveBuff,
    ConsumeBarrier,
    MultiplyBarrier,
    UseCard,
    Evade,
    Multiply,
    Increase,
    Cancel,
    Pickup
}
