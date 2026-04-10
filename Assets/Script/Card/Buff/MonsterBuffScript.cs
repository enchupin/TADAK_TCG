public abstract class MonsterBuffScript
{
    public abstract int BuffId { get; }

    public virtual void OnBuffApplied(TrainingBattleManager battleManager, Monster monster, int appliedAmount, int stack)
    {
    }

    public virtual void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnPlayerTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnEnemyDebuffApplied(TrainingBattleManager battleManager, Monster monster, int appliedBuffId, int amount, int stack, int crueltyStackBeforeApply)
    {
    }

    public virtual void OnMonsterHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
    }

    public virtual void OnPlayerHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
    }

    public virtual void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnMonsterLeaveCombat(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnMonsterRevived(TrainingBattleManager battleManager, Monster monster, int stack)
    {
    }

    public virtual void OnMonsterBeforeTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int stack)
    {
    }

    public virtual void OnMonsterAfterTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int damageAfterDefense, int stack)
    {
    }

    public virtual float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        return currentMultiplier;
    }

    public virtual bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        return false;
    }

    public virtual bool ShouldKeepBarrierOnTurnStart(TrainingBattleManager battleManager, Monster monster, int stack, bool currentShouldKeep)
    {
        return currentShouldKeep;
    }

    public virtual void OnDefenseChanged(TrainingBattleManager battleManager, Monster monster, int previousDefense, int currentDefense, int stack)
    {
    }

    public virtual int ModifyOutgoingDamage(TrainingBattleManager battleManager, Monster monster, int stack, int currentDamage)
    {
        return currentDamage;
    }

    public virtual void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
    {
    }

    public virtual int ModifyBarrierGain(TrainingBattleManager battleManager, Monster monster, int stack, int currentGain)
    {
        return currentGain;
    }

    public virtual bool CanReceiveDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int stack, bool currentCanReceive)
    {
        return currentCanReceive;
    }

    public virtual bool CanRevive(TrainingBattleManager battleManager, Monster monster, int stack, bool currentCanRevive)
    {
        return currentCanRevive;
    }

    public virtual int ResolvePersistentUpgradeCardId(TrainingBattleManager battleManager, Monster monster, int cardId, int currentCardId, int sourceBuffId, int stack)
    {
        return currentCardId;
    }
}
