using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffRuntimeService
{
    private readonly TrainingBattleManager battleManager;
    private readonly Dictionary<int, PlayerBuffScript> scripts = new();
    private readonly List<PlayerBuffScript> orderedScripts = new();

    public PlayerBuffRuntimeService(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;

        Register(new WuppiAttackSwitchBuffScript());
        Register(new WuppiGuardSwitchBuffScript());
        Register(new StrengthBuffScript());
        Register(new OverheatBuffScript());
        Register(new EnhancedCorrosionBuffScript());
        Register(new CorrosionBuffScript());
        Register(new CorrosionEnhanceBuffScript());
        Register(new StrengthContractBuffScript());
        Register(new DrawInterferenceBuffScript());
        Register(new ExtraDrawBuffScript());
        Register(new OverheatGrowthBuffScript());
        Register(new OverheatDecayBuffScript());
        Register(new AbsoluteZeroBuffScript());
        Register(new GlacierShapeOnHitBuffScript());
        Register(new PrecisionBuffScript());
        Register(new PotionEnhanceBuffScript());
        Register(new PotionCycleBuffScript());
        Register(new GlacierShapeEnhanceBuffScript());
        Register(new UnplayableUnlockBuffScript());
        Register(new DoubleJackBuffScript());
        Register(new DuplicateGenerateBuffScript());
        Register(new EfficientBarrierBuffScript());
        Register(new BarrierRetentionBuffScript());
        Register(new PermanentBarrierRetentionBuffScript());
        Register(new HighCostRepeatBuffScript());
        Register(new StraightBuffScript());
        Register(new RegenerationBuffScript());
        Register(new BurnBuffScript());
        Register(new CounterattackDecayBuffScript());
        Register(new StrengthDecayBuffScript());
        Register(new DrawLockBuffScript());
        Register(new NextCardFreeBuffScript());
        Register(new CardUseAllEnemiesDamageBuffScript());
        Register(new DamageClampToOneBuffScript());
        Register(new WeakBuffScript());
        Register(new FrailBuffScript());
        Register(new GlacierBondBuffScript());
        Register(new RetainChoiceBuffScript());
        Register(new RepeatNextCardBuffScript());
        Register(new RepeatNextPowerCardBuffScript());
        Register(new PotionCorrosionBuffScript());
        Register(new DelayedPotionFactoryBuffScript());
        Register(new BioExperimentAllBuffScript());
        Register(new PotionFactoryBuffScript());
        Register(new AttackBoostBuffScript());
        Register(new FeatherCycleBuffScript());
        Register(new FeatherEnhanceBuffScript());
        Register(new FeatherStackBoostBuffScript());
        Register(new GrowingFeatherBuffScript());
        Register(new FeatherAutoTriggerBuffScript());
        Register(new BlindFeatherBuffScript());
        Register(new DeadlyAmbushBuffScript());
        Register(new DoubleActionBuffScript());
        Register(new JokerPowerBuffScript());
        Register(new RuneBuffScript());
        Register(new WuppiGuardBuffScript());
        Register(new WuppiAttackBuffScript());
        Register(new RuneGenerationBuffScript());
        Register(new RuneBarrierBuffScript());
        Register(new FlameConductionBuffScript());
        Register(new IntimidationBuffScript());
        Register(new ThornBuffScript());
        Register(new EvadeBuffScript());
        Register(new LavaSkinBuffScript());
        Register(new TemporaryLavaSkinBuffScript());
        Register(new CounterattackBuffScript());
        Register(new ColdAirBuffScript());
        Register(new LavaBarrierBuffScript());
        Register(new ExhaustDrawContractBuffScript());
        Register(new SoulProtectionBuffScript());
    }

    public void ResetForCombat()
    {
        PlayerData player = battleManager?.playerData;
        if (player == null)
        {
            return;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            script.ResetForCombat(battleManager, player);
        }
    }

    public bool TryApplyToPlayer(int buffId, int amount)
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || amount <= 0)
        {
            return false;
        }

        return scripts.TryGetValue(buffId, out PlayerBuffScript script)
            && script.TryApplyToPlayer(battleManager, player, amount);
    }

    public void OnPlayerTurnStart()
    {
        InvokeForActiveBuffs((script, player, stack) => script.OnPlayerTurnStart(battleManager, player, stack));
    }

    public void OnPlayerTurnEnd()
    {
        InvokeForActiveBuffs((script, player, stack) => script.OnPlayerTurnEnd(battleManager, player, stack));
    }

    public bool CanPlayCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        return FoldActiveBuffs(card.CanBePlayed(), (script, player, stack, currentCanPlay) =>
            script.CanPlayCard(battleManager, player, card, stack, currentCanPlay));
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        return FoldActiveBuffs(false, (script, player, stack, currentShouldExhaust) =>
            script.ShouldExhaustUnlockedUnplayableCard(battleManager, player, card, stack, currentShouldExhaust));
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        if (card == null)
        {
            return false;
        }

        return FoldActiveBuffs(false, (script, player, stack, currentShouldDiscard) =>
            script.ShouldPotionGoToDiscardInsteadOfExhaust(battleManager, player, card, stack, currentShouldDiscard));
    }

    public bool HasPermanentBarrierRetention()
    {
        return FoldActiveBuffs(false, (script, player, stack, currentHasRetention) =>
            script.HasPermanentBarrierRetention(battleManager, player, stack, currentHasRetention));
    }

    public bool CanDrawCards()
    {
        return FoldActiveBuffs(true, (script, player, stack, currentCanDraw) =>
            script.CanDrawCards(battleManager, player, stack, currentCanDraw));
    }

    public bool CanGainCardsToHand()
    {
        return FoldActiveBuffs(true, (script, player, stack, currentCanGain) =>
            script.CanGainCardsToHand(battleManager, player, stack, currentCanGain));
    }

    public bool CanGainCardsToHandFrom(MoveZoneType from, string subject = null)
    {
        return FoldActiveBuffs(true, (script, player, stack, currentCanGain) =>
            script.CanGainCardsToHandFrom(battleManager, player, from, subject, stack, currentCanGain));
    }

    public int GetEffectiveCardCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        int effectiveCost = FoldActiveBuffs(card.cost, (script, player, stack, currentCost) =>
            script.GetEffectiveCardCost(battleManager, player, card, stack, currentCost));

        return Mathf.Max(0, effectiveCost);
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentDamage) =>
            script.GetCardUseAllEnemiesDamage(battleManager, player, stack, currentDamage)));
    }

    public int GetAdditionalBarrierGain()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentGain) =>
            script.GetAdditionalBarrierGain(battleManager, player, stack, currentGain)));
    }

    public int ResolveBarrierGain(int amount)
    {
        return Mathf.Max(0, FoldActiveBuffs(Mathf.Max(0, amount), (script, player, stack, currentGain) =>
            script.ModifyBarrierGain(battleManager, player, stack, currentGain)));
    }

    public int GetTurnStartBarrierLoss(int currentDefense)
    {
        int defense = Mathf.Max(0, currentDefense);
        return Mathf.Clamp(FoldActiveBuffs(defense, (script, player, stack, currentLoss) =>
            script.GetTurnStartBarrierLoss(battleManager, player, defense, stack, currentLoss)), 0, defense);
    }

    public bool TryConsumeBarrierRetentionOnTurnStart()
    {
        PlayerData player = battleManager?.playerData;
        if (player == null)
        {
            return false;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack <= 0)
            {
                continue;
            }

            if (script.TryConsumeBarrierRetentionOnTurnStart(battleManager, player, stack))
            {
                return true;
            }
        }

        return false;
    }

    public int GetTurnEndRetainCount()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentCount) =>
            script.GetTurnEndRetainCount(battleManager, player, stack, currentCount)));
    }

    public int ResolvePersistentUpgradeCardId(int cardId, int sourceBuffId = 0)
    {
        return FoldActiveBuffs(cardId, (script, player, stack, currentCardId) =>
            script.ResolvePersistentUpgradeCardId(battleManager, player, cardId, currentCardId, sourceBuffId, stack));
    }

    public int GetGeneratedCardDuplicateCount()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentDuplicateCount) =>
            script.GetGeneratedCardDuplicateCount(battleManager, player, stack, currentDuplicateCount)));
    }

    public int ResolveAppliedMonsterBuffId(Monster targetMonster, int buffId, int amount)
    {
        return FoldActiveBuffs(buffId, (script, player, stack, currentBuffId) =>
            script.ResolveAppliedMonsterBuffId(battleManager, player, targetMonster, buffId, amount, stack, currentBuffId));
    }

    public void ReplayTurnEndTriggeredEffects()
    {
        InvokeForActiveBuffs((script, player, stack) => script.OnPlayerTurnEndTriggered(battleManager, player, stack));
    }

    public int GetCardBaseDamageBonus(Card sourceCard, bool isAttackEffect)
    {
        int finalBonus = 0;

        InvokeForActiveBuffs((script, player, stack) =>
        {
            finalBonus = script.GetCardBaseDamageBonus(battleManager, player, sourceCard, isAttackEffect, stack, finalBonus);
        });

        return Mathf.Max(0, finalBonus);
    }

    public int ModifyCardDamage(Card sourceCard, int damage)
    {
        int finalDamage = Mathf.Max(0, damage);

        InvokeForActiveBuffs((script, player, stack) =>
        {
            finalDamage = script.ModifyCardDamage(battleManager, player, sourceCard, stack, finalDamage);
        });

        return Mathf.Max(0, finalDamage);
    }

    public int GetCalculatedCardDamageBonus(float strengthMultiplier)
    {
        int finalBonus = 0;

        InvokeForActiveBuffs((script, player, stack) =>
        {
            finalBonus = script.GetCalculatedCardDamageBonus(battleManager, player, strengthMultiplier, stack, finalBonus);
        });

        return Mathf.Max(0, finalBonus);
    }

    public float GetCalculatedCardBaseMultiplier(float baseMultiplier)
    {
        float finalMultiplier = Mathf.Max(0f, baseMultiplier);

        InvokeForActiveBuffs((script, player, stack) =>
        {
            finalMultiplier = script.GetCalculatedCardBaseMultiplier(battleManager, player, stack, finalMultiplier);
        });

        return Mathf.Max(0f, finalMultiplier);
    }

    public float GetIncomingDamageMultiplier(Monster attacker)
    {
        float multiplier = 1f;

        InvokeForActiveBuffs((script, player, stack) =>
        {
            multiplier = script.GetIncomingDamageMultiplier(battleManager, player, attacker, stack, multiplier);
        });

        return Mathf.Max(0f, multiplier);
    }

    public int ClampIncomingDamage(Monster attacker, int damage)
    {
        int finalDamage = Mathf.Max(0, damage);

        InvokeForActiveBuffs((script, player, stack) =>
        {
            finalDamage = script.ClampIncomingDamage(battleManager, player, attacker, stack, finalDamage);
        });

        return Mathf.Max(0, finalDamage);
    }

    public bool TryPreventIncomingDamage(Monster attacker, int damage)
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || damage <= 0)
        {
            return false;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack <= 0)
            {
                continue;
            }

            if (script.TryPreventIncomingDamage(battleManager, player, attacker, damage, stack))
            {
                return true;
            }
        }

        return false;
    }

    public void ConsumeIncomingDamageBuff(Monster attacker, int damage)
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || damage <= 0)
        {
            return;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack <= 0)
            {
                continue;
            }

            if (script.TryConsumeIncomingDamageBuff(battleManager, player, attacker, damage, stack))
            {
                return;
            }
        }
    }

    public float GetOutgoingDamageMultiplier()
    {
        float multiplier = 1f;

        InvokeForActiveBuffs((script, player, stack) =>
        {
            multiplier = script.GetOutgoingDamageMultiplier(battleManager, player, stack, multiplier);
        });

        return Mathf.Max(0f, multiplier);
    }

    public int ConsumeRepeatCount(Card playedCard, bool isRepeatedEffect)
    {
        int repeatCount = 0;

        InvokeForActiveBuffs((script, player, stack) =>
        {
            repeatCount += script.ConsumeRepeatCount(battleManager, player, playedCard, isRepeatedEffect, stack);
        });

        return Mathf.Max(0, repeatCount);
    }

    public void OnCardPlayed(Card playedCard, Monster originalTarget, bool isRepeatedEffect)
    {
        if (playedCard == null)
        {
            return;
        }

        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnCardPlayed(battleManager, player, playedCard, originalTarget, isRepeatedEffect, stack);
        });
    }

    public void ResolveDeferredTurnStartEffects()
    {
        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.ResolveDeferredTurnStartEffects(battleManager, player, stack);
        });
    }

    public void OnPlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnPlayerAttackResolved(battleManager, player, targetMonster, barrierBefore, barrierAfter, stack);
        });
    }

    public void OnPlayerHit(Monster attacker, int blockedDamage, int hpDamage)
    {
        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnPlayerHit(battleManager, player, attacker, blockedDamage, hpDamage, stack);
        });
    }

    public void OnPlayerBarrierReduced(int reducedAmount)
    {
        if (reducedAmount <= 0)
        {
            return;
        }

        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnPlayerBarrierReduced(battleManager, player, reducedAmount, stack);
        });
    }

    public void OnEnemyDebuffApplied(Monster targetMonster, int buffId, int amount, int crueltyStackBeforeApply)
    {
        if (targetMonster == null || targetMonster.IsDead() || amount <= 0)
        {
            return;
        }

        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnEnemyDebuffApplied(battleManager, player, targetMonster, buffId, amount, stack, crueltyStackBeforeApply);
        });
    }

    public void OnCardsExhausted(int exhaustedCount)
    {
        if (exhaustedCount <= 0)
        {
            return;
        }

        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnCardsExhausted(battleManager, player, stack, exhaustedCount);
        });
    }

    public bool TryConsumeFatalDamage()
    {
        PlayerData player = battleManager?.playerData;
        if (player == null)
        {
            return false;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack <= 0)
            {
                continue;
            }

            if (script.TryConsumeFatalDamage(battleManager, player, stack))
            {
                return true;
            }
        }

        return false;
    }

    public int ModifyFeatherApplyAmount(int amount)
    {
        return Mathf.Max(0, FoldActiveBuffs(Mathf.Max(0, amount), (script, player, stack, currentAmount) =>
            script.ModifyFeatherApplyAmount(battleManager, player, stack, currentAmount)));
    }

    public int ModifyFeatherTriggerRepeatCount(int repeatCount)
    {
        return Mathf.Max(0, FoldActiveBuffs(Mathf.Max(0, repeatCount), (script, player, stack, currentRepeatCount) =>
            script.ModifyFeatherTriggerRepeatCount(battleManager, player, stack, currentRepeatCount)));
    }

    public int GetFeatherAutoTriggerCount()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentCount) =>
            script.GetFeatherAutoTriggerCount(battleManager, player, stack, currentCount)));
    }

    public bool ShouldApplyFeatherToAllEnemies()
    {
        return FoldActiveBuffs(false, (script, player, stack, currentShouldApplyToAll) =>
            script.ShouldApplyFeatherToAllEnemies(battleManager, player, stack, currentShouldApplyToAll));
    }

    public int GetFeatherTriggerBonus()
    {
        return Mathf.Max(0, FoldActiveBuffs(0, (script, player, stack, currentBonus) =>
            script.GetFeatherTriggerBonus(battleManager, player, stack, currentBonus)));
    }

    public void OnFeatherApplied(int appliedAmount, int targetCount, TargetType targetType)
    {
        if (appliedAmount <= 0 || targetCount <= 0)
        {
            return;
        }

        InvokeForActiveBuffs((script, player, stack) =>
        {
            script.OnFeatherApplied(battleManager, player, appliedAmount, targetCount, targetType, stack);
        });
    }

    private void Register(PlayerBuffScript script)
    {
        if (script == null || script.BuffId <= 0)
        {
            return;
        }

        if (scripts.ContainsKey(script.BuffId))
        {
            return;
        }

        scripts[script.BuffId] = script;
        orderedScripts.Add(script);
    }

    private void InvokeForActiveBuffs(System.Action<PlayerBuffScript, PlayerData, int> action)
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || action == null)
        {
            return;
        }

        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack > 0)
            {
                action(script, player, stack);
            }
        }
    }

    private T FoldActiveBuffs<T>(T initialValue, System.Func<PlayerBuffScript, PlayerData, int, T, T> aggregator)
    {
        PlayerData player = battleManager?.playerData;
        if (player == null || aggregator == null)
        {
            return initialValue;
        }

        T result = initialValue;
        foreach (PlayerBuffScript script in orderedScripts)
        {
            int stack = script.GetRuntimeStack(battleManager, player);
            if (stack <= 0)
            {
                continue;
            }

            result = aggregator(script, player, stack, result);
        }

        return result;
    }
}
