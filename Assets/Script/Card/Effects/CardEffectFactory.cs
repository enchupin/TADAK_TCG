using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CardEffectData를 런타임 ICardEffect로 변환하는 팩토리
/// </summary>
public static class CardEffectFactory
{
    public static List<ICardEffect> CreateEffects(List<CardEffectData> effectDataList)
    {
        return BuildRuntimeEffects(effectDataList, null);
    }

    /// <summary>
    /// Converts CardEffectData to runtime ICardEffect.
    /// </summary>
    public static ICardEffect CreateEffect(CardEffectData effectData)
    {
        return CreateStandaloneEffect(effectData, null);
    }

    private static ICardEffect CreateStandaloneEffect(CardEffectData effectData, string inheritedSubject)
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
        // - onAction은 재귀적으로 BuildRuntimeEffects()를 호출해 체인 형태로 런타임 효과를 구성한다.
        switch (effectData.type)
        {
            case EffectType.Repeat:
                if (effectData.repeatNextEffect) {
                    Debug.LogWarning("[CardEffectFactory] Repeat 효과는 리스트 빌드 경로에서만 생성할 수 있습니다");
                }
                return null;

            case EffectType.Conditional:
                return new ConditionalEffect
                {
                    conditionData = effectData.conditionData,
                    successEffects = BuildConditionalSuccessEffects(effectData, resolvedSubject),
                    failEffects = BuildConditionalFailEffects(effectData, resolvedSubject)
                };

            case EffectType.Attack:
                return new AttackEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    cardIdList = effectData.formulaCardIdFilter,
                    target = effectData.target,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Barrier:
                return new BarrierEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    target = effectData.target,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.ExhaustCard:
                return new ExhaustCardEffect
                {
                    from = effectData.from,
                    subject = resolvedSubject,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Scry:
                return new ScryEffect { count = effectData.count > 0 ? effectData.count : effectData.amount };
            case EffectType.SelectCard:
                return new SelectCardEffect
                {
                    count = effectData.count > 0 ? effectData.count : effectData.amount,
                    from = effectData.from,
                    cardIdFilter = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter),
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Damage:
                return new DamageEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    target = effectData.target,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Draw:
                return new DrawEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.DrawCharacter:
                return new DrawCharacterEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.DrawBasic:
                return new DrawBasicEffect { amount = effectData.amount, amountFormula = effectData.amountFormula };
            case EffectType.Move:
                return new MoveEffect
                {
                    from = effectData.from,
                    to = effectData.to,
                    position = effectData.position,
                    subject = resolvedSubject,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };
            case EffectType.Copy:
                return new CopyEffect
                {
                    from = effectData.from,
                    to = effectData.to,
                    position = effectData.position,
                    subject = resolvedSubject,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    cardIdList = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter)
                };
            case EffectType.Buff:
                return new BuffEffect
                {
                    stat = effectData.stat,
                    buffId = effectData.buffId,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    duration = effectData.duration,
                    target = effectData.target
                };
            case EffectType.Heal:
                return new HealEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    cardIdList = effectData.formulaCardIdFilter,
                    target = effectData.target
                };
            case EffectType.GenerateCard:
                return new GenerateCardEffect
                {
                    RandomCard = effectData.RandomCard,
                    cardId = effectData.cardId,
                    cardIdList = effectData.formulaCardIdFilter,
                    target = effectData.target,
                    position = effectData.position
                };
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
            case EffectType.Trigger:
                return new TriggerEffect { timing = effectData.timing };
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

        for (int i = 0; i < sourceEffects.Count; i++)
        {
            if (TryBuildRuntimeEffect(sourceEffects, ref i, inheritedSubject, out ICardEffect effect) && effect != null)
            {
                runtimeEffects.Add(effect);
            }
        }

        return runtimeEffects;
    }

    private static bool TryBuildRuntimeEffect(List<CardEffectData> sourceEffects, ref int index, string inheritedSubject, out ICardEffect effect)
    {
        effect = null;
        if (sourceEffects == null || index < 0 || index >= sourceEffects.Count)
        {
            return false;
        }

        CardEffectData effectData = sourceEffects[index];
        if (effectData == null)
        {
            return false;
        }

        if (effectData.type == EffectType.Repeat && effectData.repeatNextEffect)
        {
            if (index + 1 >= sourceEffects.Count)
            {
                Debug.LogWarning("[CardEffectFactory] Repeat 효과 뒤에는 반복할 다음 이펙트가 필요합니다");
                return false;
            }

            string repeatSubject = ResolveSubject(effectData.subject, inheritedSubject);
            int nestedIndex = index + 1;
            bool built = TryBuildRuntimeEffect(sourceEffects, ref nestedIndex, repeatSubject, out ICardEffect repeatedEffect);
            index = nestedIndex;

            if (!built || repeatedEffect == null)
            {
                Debug.LogWarning("[CardEffectFactory] Repeat 효과의 다음 이펙트를 생성하지 못했습니다");
                return false;
            }

            effect = new RepeatEffect
            {
                amount = effectData.amount,
                amountFormula = effectData.amountFormula,
                effectToRepeat = repeatedEffect
            };
            return true;
        }

        effect = CreateStandaloneEffect(effectData, inheritedSubject);
        return effect != null;
    }

    private static List<ICardEffect> BuildConditionalSuccessEffects(CardEffectData effectData, string inheritedSubject)
    {
        // 조건 성공 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.successEffects (다중)
        // 2) 레거시 subEffects (다중)
        // 즉, 신규 구조를 우선 사용하고, 없으면 레거시 필드로 폴백한다.
        if (effectData.conditionData != null
            && effectData.conditionData.successEffects != null
            && effectData.conditionData.successEffects.Count > 0)
        {
            return BuildRuntimeEffects(effectData.conditionData.successEffects, inheritedSubject);
        }

        return BuildRuntimeEffects(effectData.subEffects, inheritedSubject);
    }

    private static List<ICardEffect> BuildConditionalFailEffects(CardEffectData effectData, string inheritedSubject)
    {
        // 조건 실패 시 실행할 효과 리스트를 구성한다.
        // 우선순위:
        // 1) conditionData.elseEffects (다중)
        // 성공/실패 모두 동일한 패턴으로 구성해 데이터 스키마 변화에 유연하게 대응한다.
        if (effectData.conditionData != null
            && effectData.conditionData.elseEffects != null
            && effectData.conditionData.elseEffects.Count > 0)
        {
            return BuildRuntimeEffects(effectData.conditionData.elseEffects, inheritedSubject);
        }

        return new List<ICardEffect>();
    }

    private static string ResolveSubject(string effectSubject, string inheritedSubject)
    {
        if (!string.IsNullOrWhiteSpace(effectSubject)) {
            return effectSubject;
        }

        return inheritedSubject;
    }
}
