using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CardEffectData를 런타임 ICardEffect로 변환하는 팩토리
/// </summary>
public static class CardEffectFactory
{
    public static List<ICardEffect> CreateEffects(List<CardEffectData> effectDataList)
    {
        List<CardEffectData> playableEffects = new List<CardEffectData>();
        if (effectDataList != null)
        {
            foreach (CardEffectData effectData in effectDataList)
            {
                if (effectData == null || effectData.type == EffectType.Keep)
                {
                    continue;
                }

                playableEffects.Add(effectData);
            }
        }

        return BuildRuntimeEffects(playableEffects, null);
    }

    public static List<ICardEffect> CreateKeepEffects(List<CardEffectData> effectDataList)
    {
        List<ICardEffect> keepEffects = new List<ICardEffect>();
        if (effectDataList == null)
        {
            return keepEffects;
        }

        foreach (CardEffectData effectData in effectDataList)
        {
            if (effectData == null || effectData.type != EffectType.Keep)
            {
                continue;
            }

            keepEffects.AddRange(BuildRuntimeEffects(effectData.subEffects, "ThisCard"));
        }

        return keepEffects;
    }

    /// <summary>
    /// CardEffectData를 단일 런타임 이펙트로 변환
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

        // 직렬화된 CardEffectData를 실제 실행 객체로 변환
        // onAction은 BuildRuntimeEffects로 재귀 변환해서 실행 체인으로 연결
        switch (effectData.type)
        {
            case EffectType.Repeat:
                if (effectData.repeatNextEffect)
                {
                    Debug.LogWarning("[CardEffectFactory] Repeat 이펙트는 리스트 빌드 경로에서만 생성됩니다");
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

            case EffectType.ChangeStat:
                return new ChangeStatEffect
                {
                    stat = effectData.stat,
                    change = effectData.change,
                    subject = resolvedSubject,
                    buffId = effectData.buffId,
                    buffTypes = effectData.buffTypes == null ? null : new List<int>(effectData.buffTypes),
                    random = effectData.random,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    target = effectData.target,
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
                    cardIdList = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter),
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };

            case EffectType.Cost:
                return new CostEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    subject = resolvedSubject
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
                return new DrawEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };

            case EffectType.DrawBasic:
                return new DrawBasicEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };

            case EffectType.DrawCharacter:
                return new DrawCharacterEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
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

            case EffectType.GenerateCard:
                return new GenerateCardEffect
                {
                    RandomCard = effectData.RandomCard,
                    cardId = effectData.cardId,
                    cardIdList = effectData.formulaCardIdFilter,
                    target = effectData.target,
                    position = effectData.position
                };

            case EffectType.Heal:
                return new HealEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    cardIdList = effectData.formulaCardIdFilter,
                    target = effectData.target
                };

            case EffectType.Keyword:
                return new KeywordEffect
                {
                    keyword = effectData.keyword,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula
                };

            case EffectType.Kill:
                return new KillEffect
                {
                    target = effectData.target
                };

            case EffectType.Keep:
                return null;

            case EffectType.ExtraTurn:
                return new ExtraTurnEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula
                };

            case EffectType.MixBuff:
                return new MixBuffEffect
                {
                    target = effectData.target
                };

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

            case EffectType.MultiplyBarrier:
                return new MultiplyBarrierEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    target = effectData.target
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

            case EffectType.ReduceCost:
                return new ReduceCostEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    durationText = effectData.durationText,
                    subject = resolvedSubject
                };

            case EffectType.RemoveBuff:
                return new RemoveBuffEffect
                {
                    buffId = effectData.buffId,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    target = effectData.target
                };

            case EffectType.Scry:
                return new ScryEffect
                {
                    count = effectData.count > 0 ? effectData.count : effectData.amount
                };

            case EffectType.SelectCard:
                return new SelectCardEffect
                {
                    count = effectData.count > 0 ? effectData.count : effectData.amount,
                    from = effectData.from,
                    cardIdFilter = effectData.formulaCardIdFilter == null ? null : new List<int>(effectData.formulaCardIdFilter),
                    onActions = BuildRuntimeEffects(effectData.onAction, resolvedSubject)
                };

            case EffectType.ModifyCard:
                return new ModifyCardEffect
                {
                    source = effectData.source,
                    subject = resolvedSubject,
                    param = effectData.param,
                    operation = effectData.operation,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    effectIndex = effectData.effectIndex,
                    durationText = effectData.durationText
                };

            case EffectType.ModifyCards:
                return new ModifyCardsEffect
                {
                    source = effectData.source,
                    subject = resolvedSubject,
                    param = effectData.param,
                    operation = effectData.operation,
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula,
                    effectIndex = effectData.effectIndex,
                    durationText = effectData.durationText
                };

            case EffectType.Stamina:
                return new StaminaEffect
                {
                    amount = effectData.amount,
                    amountFormula = effectData.amountFormula
                };

            case EffectType.Upgrade:
                return new UpgradeEffect
                {
                    subject = resolvedSubject,
                    upgrade = effectData.upgrade
                };

            case EffectType.Trigger:
                return new TriggerEffect
                {
                    timing = effectData.timing
                };

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
                Debug.LogWarning("[CardEffectFactory] Repeat 이펙트 뒤에는 반복할 다음 이펙트가 필요합니다");
                return false;
            }

            string repeatSubject = ResolveSubject(effectData.subject, inheritedSubject);
            int nestedIndex = index + 1;
            bool built = TryBuildRuntimeEffect(sourceEffects, ref nestedIndex, repeatSubject, out ICardEffect repeatedEffect);
            index = nestedIndex;

            if (!built || repeatedEffect == null)
            {
                Debug.LogWarning("[CardEffectFactory] Repeat 이펙트의 다음 이펙트를 생성하지 못했습니다");
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
        // 조건 성공 시 실행할 이펙트 목록
        // 우선순위
        // 1) conditionData.successEffects
        // 2) 기존 subEffects
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
        // 조건 실패 시 실행할 이펙트 목록
        // 우선순위는 conditionData.elseEffects
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
        if (!string.IsNullOrWhiteSpace(effectSubject))
        {
            return effectSubject;
        }

        return inheritedSubject;
    }
}
