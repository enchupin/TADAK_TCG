using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public class FeatherBuffRuntime
{
    private const int FeatherCardId = 203080;
    private const int EnhancedFeatherCardId = 203081;
    private readonly TrainingBattleManager battleManager;
    private bool hasTriggeredFeatherThisTurn;

    public FeatherBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void ResetForCombat()
    {
        hasTriggeredFeatherThisTurn = false;
    }

    public void OnTurnStart()
    {
        hasTriggeredFeatherThisTurn = false;
    }

    public bool TryApplyToPlayer(int buffId, int amount)
    {
        if (buffId != FeatherBuffId || battleManager?.playerData == null || amount <= 0)
        {
            return false;
        }

        int finalAmount = GetAdjustedApplyAmount(amount);
        if (finalAmount <= 0)
        {
            return true;
        }

        battleManager.playerData.AddBuff(FeatherBuffId, finalAmount);
        OnFeatherUsed();
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
        if (battleManager == null || repeatCount <= 0)
        {
            return 0;
        }

        int triggerBonus = ConsumeDeadlyAmbushBonusIfNeeded();
        switch (NormalizeTarget(target))
        {
            case TargetType.Self:
                return TriggerOnPlayerInternal(repeatCount, triggerBonus);

            case TargetType.AllEnemies:
                return TriggerOnAllEnemies(repeatCount, triggerBonus);

            default:
                return TriggerOnMonsterInternal(ResolveSingleEnemyTarget(), repeatCount, triggerBonus);
        }
    }

    public int TriggerOnMonster(Monster monster, int repeatCount = 1)
    {
        return TriggerOnMonsterInternal(monster, repeatCount, ConsumeDeadlyAmbushBonusIfNeeded());
    }

    public int TriggerOnPlayer(int repeatCount = 1)
    {
        return TriggerOnPlayerInternal(repeatCount, ConsumeDeadlyAmbushBonusIfNeeded());
    }

    public int TriggerUntilEmpty(TargetType target)
    {
        if (battleManager == null)
        {
            return 0;
        }

        int triggerBonus = ConsumeDeadlyAmbushBonusIfNeeded();
        switch (NormalizeTarget(target))
        {
            case TargetType.Self:
                return TriggerOnPlayerUntilEmpty(triggerBonus);

            case TargetType.AllEnemies:
                return TriggerOnAllEnemiesUntilEmpty(triggerBonus);

            default:
                return TriggerOnMonsterUntilEmpty(ResolveSingleEnemyTarget(), triggerBonus);
        }
    }

    public int ReplayExhaustedFeathers()
    {
        if (battleManager?.usableDeckManager == null)
        {
            return 0;
        }

        List<Card> exhaustedCards = battleManager.usableDeckManager.GetExhaustPile();
        if (exhaustedCards == null || exhaustedCards.Count == 0)
        {
            return 0;
        }

        List<Card> exhaustedFeathers = new List<Card>();
        foreach (Card card in exhaustedCards)
        {
            if (IsReplayableFeatherCard(card))
            {
                exhaustedFeathers.Add(card);
            }
        }

        if (exhaustedFeathers.Count == 0)
        {
            return 0;
        }

        int replayCount = 0;
        Monster originalTarget = battleManager.currentTarget;
        Monster replayTarget = ResolveRandomEnemyTarget();
        foreach (Card featherCard in exhaustedFeathers)
        {
            if (replayTarget != null && replayTarget.IsDead())
            {
                replayTarget = ResolveRandomEnemyTarget();
            }

            if (replayTarget == null)
            {
                break;
            }

            ExecuteExhaustedFeatherCard(featherCard, replayTarget);
            replayCount++;

            if (battleManager.TryHandleCombatEnd())
            {
                break;
            }
        }

        battleManager.currentTarget = originalTarget;

        if (replayCount > 0)
        {
            battleManager.UpdateAllUI();
        }

        return replayCount;
    }

    private void ApplyToMonsters(List<Monster> targets, int amount)
    {
        if (targets == null || targets.Count == 0 || amount <= 0)
        {
            return;
        }

        int finalAmount = GetAdjustedApplyAmount(amount);
        if (finalAmount <= 0)
        {
            return;
        }

        int autoTriggerCount = GetPlayerBuffStack(FeatherAutoTriggerBuffId);
        int triggerBonus = autoTriggerCount > 0 ? ConsumeDeadlyAmbushBonusIfNeeded() : 0;
        bool appliedAny = false;

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
                TriggerOnMonsterInternal(monster, autoTriggerCount, triggerBonus);
            }

            appliedAny = true;
        }

        if (appliedAny)
        {
            OnFeatherUsed();
        }
    }

    private int TriggerOnMonsterInternal(Monster monster, int repeatCount, int triggerBonus)
    {
        int finalRepeatCount = GetAdjustedEnemyTriggerCount(repeatCount);
        if (monster == null || monster.IsDead() || finalRepeatCount <= 0)
        {
            return 0;
        }

        int totalDamage = 0;
        for (int i = 0; i < finalRepeatCount; i++)
        {
            int featherStack = monster.GetBuffStack(BattleRuntimeDefinitions.FeatherBuffId);
            if (featherStack <= 0)
            {
                break;
            }

            int dealtDamage = monster.TakeDamage(featherStack + triggerBonus, 0);
            monster.ConsumeBuffStack(BattleRuntimeDefinitions.FeatherBuffId, 1);
            if (dealtDamage > 0)
            {
                totalDamage += dealtDamage;
                battleManager.battleContext?.OnDamageDealt(dealtDamage);
            }
        }

        return totalDamage;
    }

    private int TriggerOnPlayerInternal(int repeatCount, int triggerBonus)
    {
        PlayerData player = battleManager.playerData;
        if (player == null || repeatCount <= 0)
        {
            return 0;
        }

        int totalDamage = 0;
        for (int i = 0; i < repeatCount; i++)
        {
            int featherStack = player.GetBuffStack(BattleRuntimeDefinitions.FeatherBuffId);
            if (featherStack <= 0)
            {
                break;
            }

            totalDamage += player.TakeDamage(featherStack + triggerBonus);
            player.ConsumeBuffStack(BattleRuntimeDefinitions.FeatherBuffId, 1);
        }

        return totalDamage;
    }

    private int TriggerOnMonsterUntilEmpty(Monster monster, int triggerBonus)
    {
        if (monster == null || monster.IsDead())
        {
            return 0;
        }

        int totalDamage = 0;
        while (!monster.IsDead() && monster.GetBuffStack(FeatherBuffId) > 0)
        {
            totalDamage += TriggerOnMonsterInternal(monster, 1, triggerBonus);
        }

        return totalDamage;
    }

    private int TriggerOnPlayerUntilEmpty(int triggerBonus)
    {
        PlayerData player = battleManager.playerData;
        if (player == null)
        {
            return 0;
        }

        int totalDamage = 0;
        while (player.GetBuffStack(FeatherBuffId) > 0)
        {
            totalDamage += TriggerOnPlayerInternal(1, triggerBonus);
        }

        return totalDamage;
    }

    private int TriggerOnAllEnemies(int repeatCount, int triggerBonus)
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return 0;
        }

        int totalDamage = 0;
        foreach (Monster monster in livingMonsters)
        {
            totalDamage += TriggerOnMonsterInternal(monster, repeatCount, triggerBonus);
        }

        return totalDamage;
    }

    private int TriggerOnAllEnemiesUntilEmpty(int triggerBonus)
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return 0;
        }

        int totalDamage = 0;
        foreach (Monster monster in livingMonsters)
        {
            totalDamage += TriggerOnMonsterUntilEmpty(monster, triggerBonus);
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

    private static TargetType NormalizeTarget(TargetType target)
    {
        return target == TargetType.None ? TargetType.SingleEnemy : target;
    }

    private void OnFeatherUsed()
    {
        int featherCycle = GetPlayerBuffStack(FeatherCycleBuffId);
        if (featherCycle > 0)
        {
            battleManager.DrawCards(featherCycle);
        }
    }

    private void AutoTriggerPlayerIfNeeded()
    {
        int autoTriggerCount = GetPlayerBuffStack(FeatherAutoTriggerBuffId);
        if (autoTriggerCount <= 0)
        {
            return;
        }

        TriggerOnPlayerInternal(autoTriggerCount, ConsumeDeadlyAmbushBonusIfNeeded());
    }

    private int GetAdjustedApplyAmount(int amount)
    {
        return amount + GetPlayerBuffStack(FeatherStackBoostBuffId);
    }

    private int GetAdjustedEnemyTriggerCount(int repeatCount)
    {
        if (repeatCount <= 0)
        {
            return 0;
        }

        return repeatCount + GetPlayerBuffStack(FeatherRepeatBuffId);
    }

    private bool ShouldApplyToAllEnemies()
    {
        return GetPlayerBuffStack(BlindFeatherBuffId) > 0;
    }

    private int ConsumeDeadlyAmbushBonusIfNeeded()
    {
        int deadlyAmbush = !hasTriggeredFeatherThisTurn ? GetPlayerBuffStack(DeadlyAmbushBuffId) : 0;
        hasTriggeredFeatherThisTurn = true;
        return deadlyAmbush;
    }

    private int GetPlayerBuffStack(int buffId)
    {
        return battleManager?.playerData != null ? battleManager.playerData.GetBuffStack(buffId) : 0;
    }

    private void ExecuteExhaustedFeatherCard(Card featherCard, Monster targetMonster)
    {
        if (featherCard == null || targetMonster == null || targetMonster.IsDead())
        {
            return;
        }

        BattleContext context = battleManager.battleContext;
        List<Card> previousThisCardContext = context?.GetContextCards("ThisCard");
        List<Card> previousSelfContext = context?.GetContextCards("Self");
        Monster previousTarget = battleManager.currentTarget;

        if (context != null)
        {
            List<Card> cardContext = new List<Card> { featherCard };
            context.SetContextCards("ThisCard", cardContext);
            context.SetContextCards("Self", cardContext);
        }

        battleManager.currentTarget = targetMonster;
        featherCard.Play(battleManager);
        battleManager.currentTarget = previousTarget;

        RestoreContextCards(context, "ThisCard", previousThisCardContext);
        RestoreContextCards(context, "Self", previousSelfContext);
    }

    private static void RestoreContextCards(BattleContext context, string subject, List<Card> cards)
    {
        if (context == null)
        {
            return;
        }

        if (cards == null || cards.Count == 0)
        {
            context.ClearContextCards(subject);
            return;
        }

        context.SetContextCards(subject, cards);
    }

    private static bool IsReplayableFeatherCard(Card card)
    {
        return card != null
            && (card.cardId == FeatherCardId || card.cardId == EnhancedFeatherCardId);
    }
}
