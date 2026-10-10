using System.Collections;
using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class FeatherBuffScript : PlayerBuffScript
{
    private readonly TrainingBattleManager battleManager;
    private readonly PlayerBuffRuntimeService playerBuffRuntimeService;

    public override int BuffId => FeatherBuffId;

    public FeatherBuffScript(TrainingBattleManager battleManager, PlayerBuffRuntimeService playerBuffRuntimeService)
    {
        this.battleManager = battleManager;
        this.playerBuffRuntimeService = playerBuffRuntimeService;
    }

    public bool TryApplyToPlayer(int buffId, int amount)
    {
        if (buffId != FeatherBuffId || battleManager?.playerData == null || amount <= 0)
        {
            return false;
        }

        int finalAmount = playerBuffRuntimeService != null
            ? playerBuffRuntimeService.ModifyFeatherApplyAmount(amount)
            : amount;
        if (finalAmount <= 0)
        {
            return true;
        }

        battleManager.playerData.AddBuff(FeatherBuffId, finalAmount);
        NotifyFeatherApplied(finalAmount, 1, TargetType.Self);
        AutoTriggerPlayerIfNeeded();
        return true;
    }

    public bool TryApplyToMonster(int buffId, Monster targetMonster, int amount)
    {
        if (buffId != FeatherBuffId)
        {
            return false;
        }

        ApplyToMonsters(ResolveApplicationTargets(targetMonster), amount);
        return true;
    }

    public bool TryApplyToAllEnemies(int buffId, int amount)
    {
        if (buffId != FeatherBuffId)
        {
            return false;
        }

        ApplyToMonsters(battleManager.GetLivingMonsters(), amount);
        return true;
    }

    public int Trigger(TargetType target, int repeatCount)
    {
        if (battleManager == null || repeatCount <= 0 || !HasFeatherTarget(target))
        {
            return 0;
        }

        switch (NormalizeTarget(target))
        {
            case TargetType.Self:
                return TriggerOnPlayerInternal(repeatCount);

            case TargetType.AllEnemies:
                return TriggerOnAllEnemies(repeatCount);

            default:
                return TriggerOnMonsterInternal(ResolveSingleEnemyTarget(), repeatCount);
        }
    }

    public int TriggerOnMonster(Monster monster, int repeatCount = 1)
    {
        if (repeatCount <= 0 || !HasFeather(monster)) return 0;
        return TriggerOnMonsterInternal(monster, repeatCount);
    }

    public int TriggerUntilEmpty(TargetType target)
    {
        if (battleManager == null || !HasFeatherTarget(target))
        {
            return 0;
        }

        switch (NormalizeTarget(target))
        {
            case TargetType.Self:
                return TriggerOnPlayerUntilEmpty();

            case TargetType.AllEnemies:
                return TriggerOnAllEnemiesUntilEmpty();

            default:
                return TriggerOnMonsterUntilEmpty(ResolveSingleEnemyTarget());
        }
    }

    public IEnumerator ReplayExhaustedFeathersSequence()
    {
        if (battleManager?.usableDeckManager == null) yield break;
        List<Card> candidates = new(battleManager.usableDeckManager.GetExhaustPile());
        foreach (Card exhaustedCard in candidates)
        {
            if (!BuffCardUtility.IsFeatherCard(exhaustedCard)) continue;
            Monster target = ResolveRandomEnemyTarget();
            if (target == null) break;
            Card copy = exhaustedCard.CloneForRuntimeCopy();
            if (copy == null) continue;

            yield return TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
                battleManager, copy, target, resolveDestination: false,
                triggerPowerEffects: true, allowRepeats: true, applyPostPlayKeywords: false);
            if (battleManager.TryHandleCombatEnd()) break;
        }
        battleManager.UpdateAllUI();
    }

    private void ApplyToMonsters(List<Monster> targets, int amount)
    {
        if (targets == null || targets.Count == 0 || amount <= 0)
        {
            return;
        }

        int finalAmount = playerBuffRuntimeService != null
            ? playerBuffRuntimeService.ModifyFeatherApplyAmount(amount)
            : amount;
        if (finalAmount <= 0)
        {
            return;
        }

        int autoTriggerCount = playerBuffRuntimeService != null
            ? playerBuffRuntimeService.GetFeatherAutoTriggerCount()
            : 0;
        int appliedTargetCount = 0;

        foreach (Monster monster in targets)
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            int crueltyStackBeforeApply = monster.GetBuffStack(CrueltyDebuffId);
            monster.AddBuff(FeatherBuffId, finalAmount);
            battleManager.HandleEnemyDebuffApplied(monster, FeatherBuffId, finalAmount, crueltyStackBeforeApply);
            if (autoTriggerCount > 0)
            {
                TriggerOnMonsterInternal(monster, autoTriggerCount);
            }

            appliedTargetCount++;
        }

        if (appliedTargetCount > 0)
        {
            NotifyFeatherApplied(finalAmount, appliedTargetCount, TargetType.AllEnemies);
        }
    }

    private int TriggerOnMonsterInternal(Monster monster, int repeatCount)
    {
        int finalRepeatCount = playerBuffRuntimeService != null
            ? playerBuffRuntimeService.ModifyFeatherTriggerRepeatCount(repeatCount)
            : repeatCount;
        if (monster == null || monster.IsDead() || finalRepeatCount <= 0)
        {
            return 0;
        }

        int totalDamage = 0;
        for (int i = 0; i < finalRepeatCount; i++)
        {
            int featherStack = monster.GetBuffStack(FeatherBuffId);
            if (featherStack <= 0 || monster.IsDead())
            {
                break;
            }

            int damage = battleManager.ResolvePlayerEffectDamage(featherStack);
            int dealtDamage = damage > 0 ? monster.TakeDamage(damage, 0) : 0;
            monster.ConsumeBuffStack(FeatherBuffId, 1);
            if (dealtDamage > 0)
            {
                battleManager.Buffs.Player.OnPlayerDamageDealt(monster, dealtDamage);
                totalDamage += dealtDamage;
                battleManager.battleContext?.OnDamageDealt(dealtDamage);
            }
        }

        return totalDamage;
    }

    private int TriggerOnPlayerInternal(int repeatCount)
    {
        PlayerData player = battleManager.playerData;
        if (player == null || repeatCount <= 0)
        {
            return 0;
        }

        int totalDamage = 0;
        for (int i = 0; i < repeatCount; i++)
        {
            int featherStack = player.GetBuffStack(FeatherBuffId);
            if (featherStack <= 0)
            {
                break;
            }

            totalDamage += player.TakeDamage(featherStack);
            player.ConsumeBuffStack(FeatherBuffId, 1);
        }

        return totalDamage;
    }

    private int TriggerOnMonsterUntilEmpty(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return 0;
        }

        int totalDamage = 0;
        while (!monster.IsDead() && monster.GetBuffStack(FeatherBuffId) > 0)
        {
            totalDamage += TriggerOnMonsterInternal(monster, 1);
        }

        return totalDamage;
    }

    private int TriggerOnPlayerUntilEmpty()
    {
        PlayerData player = battleManager.playerData;
        if (player == null)
        {
            return 0;
        }

        int totalDamage = 0;
        while (player.GetBuffStack(FeatherBuffId) > 0)
        {
            totalDamage += TriggerOnPlayerInternal(1);
        }

        return totalDamage;
    }

    private int TriggerOnAllEnemies(int repeatCount)
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return 0;
        }

        int totalDamage = 0;
        foreach (Monster monster in livingMonsters)
        {
            totalDamage += TriggerOnMonsterInternal(monster, repeatCount);
        }

        return totalDamage;
    }

    private int TriggerOnAllEnemiesUntilEmpty()
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return 0;
        }

        int totalDamage = 0;
        foreach (Monster monster in livingMonsters)
        {
            totalDamage += TriggerOnMonsterUntilEmpty(monster);
        }

        return totalDamage;
    }

    private List<Monster> ResolveApplicationTargets(Monster targetMonster)
    {
        if (ShouldApplyToAllEnemies())
        {
            return battleManager.GetLivingMonsters();
        }

        if (targetMonster == null || targetMonster.IsDead())
        {
            return new List<Monster>();
        }

        return new List<Monster> { targetMonster };
    }

    private Monster ResolveSingleEnemyTarget()
    {
        if (battleManager.currentTarget != null && !battleManager.currentTarget.IsDead())
        {
            return battleManager.currentTarget;
        }

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count != 1)
        {
            return null;
        }

        return livingMonsters[0];
    }

    private Monster ResolveRandomEnemyTarget()
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return null;
        }

        return livingMonsters[UnityEngine.Random.Range(0, livingMonsters.Count)];
    }

    private TargetType NormalizeTarget(TargetType target)
    {
        if (ShouldApplyToAllEnemies() && (target == TargetType.None || target == TargetType.SingleEnemy || target == TargetType.RandomEnemy))
            return TargetType.AllEnemies;
        return target == TargetType.None ? TargetType.SingleEnemy : target;
    }

    private void AutoTriggerPlayerIfNeeded()
    {
        int autoTriggerCount = playerBuffRuntimeService != null
            ? playerBuffRuntimeService.GetFeatherAutoTriggerCount()
            : 0;
        if (autoTriggerCount <= 0)
        {
            return;
        }

        TriggerOnPlayerInternal(autoTriggerCount);
    }

    private bool ShouldApplyToAllEnemies()
    {
        return playerBuffRuntimeService != null && playerBuffRuntimeService.ShouldApplyFeatherToAllEnemies();
    }

    private static bool HasFeather(Monster monster)
    {
        return monster != null && !monster.IsDead() && monster.GetBuffStack(FeatherBuffId) > 0;
    }

    private bool HasFeatherTarget(TargetType target)
    {
        if (battleManager == null) return false;
        switch (NormalizeTarget(target))
        {
            case TargetType.Self:
                return battleManager.playerData != null && battleManager.playerData.hp > 0
                    && battleManager.playerData.GetBuffStack(FeatherBuffId) > 0;
            case TargetType.AllEnemies:
                return battleManager.GetLivingMonsters().Exists(HasFeather);
            default:
                return HasFeather(ResolveSingleEnemyTarget());
        }
    }

    private void NotifyFeatherApplied(int appliedAmount, int targetCount, TargetType targetType)
    {
        if (playerBuffRuntimeService == null || appliedAmount <= 0 || targetCount <= 0)
        {
            return;
        }

        playerBuffRuntimeService.OnFeatherApplied(appliedAmount, targetCount, targetType);
    }
}
