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
    // onAction 실행 문맥(전달 데이터 식별자)
    // JSON의 subject 값을 그대로 보존하기 위한 필드.
    // 예: SelectedCard, DrawnCard, GeneratedCard, LostBarrier, UnblockedDamage
    // 현재 CreateEffect() 단계에서는 런타임 Effect 인스턴스로 직접 주입하지 않고,
    // 데이터/디버깅/후속 확장(런타임 라우팅) 용도로 저장한다.
    public string subject;

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
        // 핵심 역할:
        // - 직렬화된 데이터(CardEffectData)를 런타임 실행 객체(ICardEffect)로 변환한다.
        // - type에 따라 필요한 필드만 골라 각 Effect 생성자 형태로 전달한다.
        // 주의:
        // - JSON/에셋에는 다양한 필드가 공존하지만, 실제 실행 시에는 각 효과가 사용하는 필드만 의미가 있다.
        // - onAction은 재귀적으로 CreateEffect()를 호출해 체인 형태로 런타임 효과를 구성한다.
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
        // 조건 성공 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.successEffects (다중)
        // 2) conditionData.successEffect (단일)
        // 3) 레거시 subEffects (다중)
        // 즉, 신규 구조를 우선 사용하고, 없으면 레거시 필드로 폴백한다.
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
        // 조건 실패 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.elseEffects (다중)
        // 2) conditionData.failEffect (단일)
        // 3) 레거시 failEffect (단일)
        // 성공/실패 모두 동일한 패턴으로 구성해 데이터 스키마 변화에 유연하게 대응한다.
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
