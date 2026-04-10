public abstract class PlayerBuffScript
{
    public abstract int BuffId { get; }

    public virtual int GetRuntimeStack(TrainingBattleManager battleManager, PlayerData player)
    {
        return player != null ? player.GetBuffStack(BuffId) : 0;
    }

    public virtual void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
    }

    public virtual bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        return false;
    }

    public virtual void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
    }

    public virtual void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
    }

    public virtual void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
    }

    public virtual bool CanPlayCard(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentCanPlay)
    {
        return currentCanPlay;
    }

    public virtual bool ShouldExhaustUnlockedUnplayableCard(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentShouldExhaust)
    {
        return currentShouldExhaust;
    }

    public virtual bool ShouldPotionGoToDiscardInsteadOfExhaust(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentShouldDiscard)
    {
        return currentShouldDiscard;
    }

    public virtual bool HasPermanentBarrierRetention(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentHasRetention)
    {
        return currentHasRetention;
    }

    public virtual bool CanDrawCards(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentCanDraw)
    {
        return currentCanDraw;
    }

    public virtual bool CanGainCardsToHand(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentCanGain)
    {
        return currentCanGain;
    }

    public virtual bool CanGainCardsToHandFrom(TrainingBattleManager battleManager, PlayerData player, MoveZoneType from, string subject, int stack, bool currentCanGain)
    {
        return currentCanGain;
    }

    public virtual int GetEffectiveCardCost(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, int currentCost)
    {
        return currentCost;
    }

    public virtual int GetCardUseAllEnemiesDamage(TrainingBattleManager battleManager, PlayerData player, int stack, int currentDamage)
    {
        return currentDamage;
    }

    public virtual int GetAdditionalBarrierGain(TrainingBattleManager battleManager, PlayerData player, int stack, int currentGain)
    {
        return currentGain;
    }

    public virtual int ModifyBarrierGain(TrainingBattleManager battleManager, PlayerData player, int stack, int currentGain)
    {
        return currentGain;
    }

    public virtual int GetTurnStartBarrierLoss(TrainingBattleManager battleManager, PlayerData player, int currentDefense, int stack, int currentLoss)
    {
        return currentLoss;
    }

    public virtual bool TryConsumeBarrierRetentionOnTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        return false;
    }

    public virtual int GetTurnEndRetainCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentCount)
    {
        return currentCount;
    }

    public virtual int ResolvePersistentUpgradeCardId(TrainingBattleManager battleManager, PlayerData player, int cardId, int currentCardId, int sourceBuffId, int stack)
    {
        return currentCardId;
    }

    public virtual int GetGeneratedCardDuplicateCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentDuplicateCount)
    {
        return currentDuplicateCount;
    }

    public virtual int ResolveAppliedMonsterBuffId(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int currentBuffId)
    {
        return currentBuffId;
    }

    public virtual int GetCardBaseDamageBonus(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, bool isAttackEffect, int stack, int currentBonus)
    {
        return currentBonus;
    }

    public virtual int ModifyCardDamage(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, int stack, int currentDamage)
    {
        return currentDamage;
    }

    public virtual int GetCalculatedCardDamageBonus(TrainingBattleManager battleManager, PlayerData player, float strengthMultiplier, int stack, int currentBonus)
    {
        return currentBonus;
    }

    public virtual float GetCalculatedCardBaseMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        return currentMultiplier;
    }

    public virtual float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, float currentMultiplier)
    {
        return currentMultiplier;
    }

    public virtual int ClampIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, int currentDamage)
    {
        return currentDamage;
    }

    public virtual bool TryPreventIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        return false;
    }

    public virtual bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        return false;
    }

    public virtual float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        return currentMultiplier;
    }

    public virtual int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        return 0;
    }

    public virtual void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
    }

    public virtual void ResolveDeferredTurnStartEffects(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
    }

    public virtual void OnPlayerAttackResolved(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int barrierBefore, int barrierAfter, int stack)
    {
    }

    public virtual void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
    }

    public virtual void OnPlayerBarrierReduced(TrainingBattleManager battleManager, PlayerData player, int reducedAmount, int stack)
    {
    }

    public virtual void OnEnemyDebuffApplied(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int buffId, int amount, int stack, int crueltyStackBeforeApply)
    {
    }

    public virtual void OnCardsExhausted(TrainingBattleManager battleManager, PlayerData player, int stack, int exhaustedCount)
    {
    }

    public virtual bool TryConsumeFatalDamage(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        return false;
    }

    public virtual int ModifyFeatherApplyAmount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentAmount)
    {
        return currentAmount;
    }

    public virtual int ModifyFeatherTriggerRepeatCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentRepeatCount)
    {
        return currentRepeatCount;
    }

    public virtual int GetFeatherAutoTriggerCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentCount)
    {
        return currentCount;
    }

    public virtual bool ShouldApplyFeatherToAllEnemies(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentShouldApplyToAll)
    {
        return currentShouldApplyToAll;
    }

    public virtual int GetFeatherTriggerBonus(TrainingBattleManager battleManager, PlayerData player, int stack, int currentBonus)
    {
        return currentBonus;
    }

    public virtual void OnFeatherApplied(TrainingBattleManager battleManager, PlayerData player, int appliedAmount, int targetCount, TargetType targetType, int stack)
    {
    }
}
