using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CardEffectData를 런타임 ICardEffect로 변환하는 팩토리.
/// </summary>
public static class CardEffectFactory
{
    /// <summary>
    /// Converts CardEffectData to runtime ICardEffect.
    /// </summary>
    public static ICardEffect CreateEffect(CardEffectData effectData)
    {
        return CreateEffect(effectData, null);
    }

    private static ICardEffect CreateEffect(CardEffectData effectData, string inheritedSubject)
    {
        if (effectData == null)
        {
            return null;
        }

        string resolvedSubject = ResolveSubject(effectData.subject, inheritedSubject);

        // 핵심 역할:
        // - 직렬화된 데이터(CardEffectData)를 런타임 실행 객체(ICardEffect)로 변환한다.
        // - type에 따라 필요한 필드만 골라 각 Effect 생성자 형태로 전달한다.
        // 주의:
        // - JSON/에셋에는 다양한 필드가 공존하지만, 실제 실행 시에는 각 효과가 사용하는 필드만 의미가 있다.
        // - onAction은 재귀적으로 CreateEffect()를 호출해 체인 형태로 런타임 효과를 구성한다.
        switch (effectData.type)
        {
            case EffectType.Repeat:
                List<ICardEffect> repeatedEffects = new List<ICardEffect>();
                if (effectData.subEffects != null)
                {
                    foreach (CardEffectData sub in effectData.subEffects)
                    {
                        ICardEffect eff = CreateEffect(sub, resolvedSubject);
                        if (eff != null) repeatedEffects.Add(eff);
                    }
                }
                return new RepeatEffect { count = effectData.count, countFormula = effectData.amountFormula, effectsToRepeat = repeatedEffects };

            case EffectType.Conditional:
                List<ICardEffect> success = BuildConditionalSuccessEffects(effectData, resolvedSubject);
                List<ICardEffect> fail = BuildConditionalFailEffects(effectData, resolvedSubject);
                return new ConditionalEffect
                {
                    conditionData = effectData.conditionData,
                    successEffects = success,
                    failEffects = fail
                };

            case EffectType.Attack:
                return new AttackEffect { amount = effectData.amount, amountFormula = effectData.amountFormula, cardIdList = effectData.formulaCardIdFilter, target = effectData.target, onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject) };
            case EffectType.Barrier:
                return new BarrierEffect { amount = effectData.amount, amountFormula = effectData.amountFormula, target = effectData.target, onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject) };
            case EffectType.DiscardHand:
                return new DiscardHandEffect { count = effectData.count, amountFormula = effectData.amountFormula, target = effectData.target };
            case EffectType.ExhaustHand:
                return new ExhaustHandEffect { amountFormula = effectData.amountFormula };
            case EffectType.Scry:
                return new ScryEffect { count = effectData.count };
            case EffectType.ChoiceHand:
                return new ChoiceHandEffect { count = effectData.count };
            case EffectType.SelectCard:
                return new SelectCardEffect
                {
                    count = effectData.count > 0 ? effectData.count : effectData.amount,
                    from = effectData.from,
                    cardIdFilter = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter),
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Damage:
                return new DamageEffect { amount = effectData.amount, amountFormula = effectData.amountFormula, target = effectData.target, onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject) };
            case EffectType.Draw:
                return new DrawEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.DrawCharacter:
                return new DrawCharacterEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.DrawBasic:
                return new DrawBasicEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.Move:
                return new MoveEffect { from = effectData.from, to = effectData.to, position = effectData.position, subject = resolvedSubject, amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.Copy:
                return new CopyEffect { from = effectData.from, to = effectData.to, position = effectData.position, subject = resolvedSubject, amount = effectData.amount, amountFormula = effectData.amountFormula, cardIdList = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter) };
            case EffectType.Buff:
                return new BuffEffect { stat = effectData.stat, buffId = effectData.buffId, amount = effectData.amount, amountFormula = effectData.amountFormula, duration = effectData.duration, target = effectData.target };
            case EffectType.Heal:
                return new HealEffect { amount = effectData.amount, amountFormula = effectData.amountFormula, cardIdList = effectData.formulaCardIdFilter, target = effectData.target };
        case EffectType.GenerateCard:
                return new GenerateCardEffect { RandomCard = effectData.RandomCard, cardId = effectData.cardId, cardIdList = effectData.formulaCardIdFilter, target = effectData.target, position = effectData.position };
            case EffectType.RandGenerate:
                return new RandGenerateEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    RandomCard = effectData.RandomCard,
                    cardIdList = effectData.formulaCardIdFilter,
                    to = ResolveRandGenerateToZone(effectData.to, effectData.target),
                    position = effectData.position
                };
            case EffectType.Keyword:
                return new KeywordEffect { keyword = effectData.keyword, amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.Pickup:
                return new PickupEffect();
            default:
                Debug.LogWarning($"[CardData] Unknown effect type: {effectData.type}");
                return null;
        }
    }

    private static MoveZoneType ResolveRandGenerateToZone(MoveZoneType to, TargetType fallbackTarget)
    {
        if (to != MoveZoneType.None)
        {
            return to;
        }

        switch (fallbackTarget)
        {
            case TargetType.Hand:
                return MoveZoneType.Hand;
            case TargetType.Discard:
                return MoveZoneType.DiscardPile;
            case TargetType.Deck:
                return MoveZoneType.DrawPile;
            default:
                return MoveZoneType.None;
        }
    }

    private static List<ICardEffect> BuildRuntimeEffects(List<CardEffectData> sourceEffects, string inheritedSubject = null)
    {
        List<ICardEffect> runtimeEffects = new List<ICardEffect>();
        if (sourceEffects == null)
        {
            return runtimeEffects;
        }

        foreach (CardEffectData nestedEffectData in sourceEffects)
        {
            ICardEffect effect = CreateEffect(nestedEffectData, inheritedSubject);
            if (effect != null)
            {
                runtimeEffects.Add(effect);
            }
        }

        return runtimeEffects;
    }

    private static List<ICardEffect> BuildConditionalSuccessEffects(CardEffectData effectData, string inheritedSubject)
    {
        // 조건 성공 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.successEffects (다중)
        // 2) 레거시 subEffects (다중)
        // 즉, 신규 구조를 우선 사용하고, 없으면 레거시 필드로 폴백한다.
        List<ICardEffect> success = new List<ICardEffect>();

        if (effectData.conditionData != null)
        {
            if (effectData.conditionData.successEffects != null && effectData.conditionData.successEffects.Count > 0)
            {
                foreach (CardEffectData nestedEffectData in effectData.conditionData.successEffects)
                {
                    ICardEffect effect = CreateEffect(nestedEffectData, inheritedSubject);
                    if (effect != null) success.Add(effect);
                }

                return success;
            }
        }

        if (effectData.subEffects != null)
        {
            foreach (CardEffectData nestedEffectData in effectData.subEffects)
            {
                ICardEffect effect = CreateEffect(nestedEffectData, inheritedSubject);
                if (effect != null) success.Add(effect);
            }
        }

        return success;
    }

    private static List<ICardEffect> BuildConditionalFailEffects(CardEffectData effectData, string inheritedSubject)
    {
        // 조건 실패 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.elseEffects (다중)
        // 성공/실패 모두 동일한 패턴으로 구성해 데이터 스키마 변화에 유연하게 대응한다.
        List<ICardEffect> fail = new List<ICardEffect>();

        if (effectData.conditionData != null)
        {
            if (effectData.conditionData.elseEffects != null && effectData.conditionData.elseEffects.Count > 0)
            {
                foreach (CardEffectData nestedEffectData in effectData.conditionData.elseEffects)
                {
                    ICardEffect effect = CreateEffect(nestedEffectData, inheritedSubject);
                    if (effect != null) fail.Add(effect);
                }

                return fail;
            }
        }

        return fail;
    }

    private static string ResolveSubject(string effectSubject, string inheritedSubject)
    {
        if (!string.IsNullOrWhiteSpace(effectSubject)) {
            return effectSubject;
        }

        return inheritedSubject;
    }
}
