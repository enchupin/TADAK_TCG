using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class AttackEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<int> cardIdList;
    public TargetType target;
    public float ampMultiplier = 1f;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, amount);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        ExecuteInternal(battleManager, forwardedAmount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        int attackBoostStack = battleManager?.playerData != null
            ? Mathf.Max(0, battleManager.playerData.GetBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId))
            : 0;
        int finalAmount = BuildFinalDamageAmount(battleManager, forwardedAmount);
        int totalDamageDealt = 0;

        switch (target)
        {
            case TargetType.AllEnemies:
                if (battleManager.spawnedMonsters.Count <= 0)
                {
                    Debug.LogWarning("[AttackEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> targets = new(battleManager.spawnedMonsters);
                if (targets == null)
                {
                    Debug.LogWarning("[AttackEffect] EnemiesList is missing. Effect cancelled.");
                    return;
                }

                bool hasValidTarget = false;
                foreach (Monster monster in targets)
                {
                    if (monster != null && !monster.IsDead())
                    {
                        hasValidTarget = true;
                        break;
                    }
                }

                if (!hasValidTarget)
                {
                    Debug.LogWarning("[AttackEffect] No living enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                ConsumeAttackBoost(battleManager, attackBoostStack);

                foreach (Monster monster in targets)
                {
                    if (monster == null || monster.IsDead())
                    {
                        continue;
                    }

                    int barrierBefore = monster.defense;
                    totalDamageDealt += monster.TakeDamage(finalAmount, 0);
                    battleManager.HandlePlayerAttackResolved(monster, barrierBefore, monster.defense);
                }
                break;

            case TargetType.SingleEnemy:
                if (battleManager.spawnedMonsters.Count <= 0)
                {
                    Debug.LogWarning("[AttackEffect] No enemies available for SingleEnemy target. Effect cancelled.");
                    break;
                }

                Monster targetMonster = battleManager.currentTarget;
                if (targetMonster == null)
                {
                    List<Monster> livingMonsters = battleManager.GetLivingMonsters();
                    if (livingMonsters != null && livingMonsters.Count == 1)
                    {
                        targetMonster = livingMonsters[0];
                    }
                }
                if (targetMonster == null)
                {
                    Debug.LogWarning("[AttackEffect] SingleEnemy target is missing. Effect cancelled.");
                    return;
                }

                ConsumeAttackBoost(battleManager, attackBoostStack);
                int targetBarrierBefore = targetMonster.defense;
                totalDamageDealt += targetMonster.TakeDamage(finalAmount, 0);
                battleManager.HandlePlayerAttackResolved(targetMonster, targetBarrierBefore, targetMonster.defense);
                break;

            case TargetType.RandomEnemy:
                List<Monster> randomTargets = battleManager.GetLivingMonsters();
                if (randomTargets == null || randomTargets.Count == 0)
                {
                    Debug.LogWarning("[AttackEffect] No enemies available for RandomEnemy target. Effect cancelled.");
                    return;
                }

                Monster randomTarget = randomTargets[Random.Range(0, randomTargets.Count)];
                ConsumeAttackBoost(battleManager, attackBoostStack);
                int randomBarrierBefore = randomTarget.defense;
                totalDamageDealt += randomTarget.TakeDamage(finalAmount, 0);
                battleManager.HandlePlayerAttackResolved(randomTarget, randomBarrierBefore, randomTarget.defense);
                break;

            case TargetType.Self:
                if (battleManager.playerData == null)
                {
                    Debug.LogWarning("[AttackEffect] PlayerData is missing. Self target effect cancelled.");
                    return;
                }

                ConsumeAttackBoost(battleManager, attackBoostStack);
                battleManager.playerData.TakeDamage(finalAmount);
                break;
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
        if (totalDamageDealt > 0 && battleManager.battleContext != null)
        {
            Debug.Log($"[AttackEffect] Damage dealt: {totalDamageDealt}, LastDamage: {battleManager.battleContext.lastDamageDealt}, ThisTurnTotal: {battleManager.battleContext.totalDamageDealt}");
        }

        if (onActions != null)
        {
            foreach (ICardEffect onAction in onActions)
            {
                onAction?.Execute(battleManager, totalDamageDealt);
            }
        }
    }

    private int BuildFinalDamageAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        Card sourceCard = battleManager?.battleContext?.GetContextCard("ThisCard")
            ?? battleManager?.battleContext?.GetLastPlayedCard();
        ResolveAttackAmount(battleManager, forwardedAmount, out int baseAmount, out float cardMultiplier);
        baseAmount += Mathf.Max(0, battleManager != null ? battleManager.GetCardBaseDamageBonus(sourceCard, true) : 0);

        if (battleManager.playerData == null)
        {
            int fallbackDamage = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            return battleManager != null
                ? battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, fallbackDamage)
                : fallbackDamage;
        }

        int resolvedDamage = battleManager.playerData.CalculateCardDamage(baseAmount, ampMultiplier, cardMultiplier);
        return battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, resolvedDamage);
    }

    private static void ConsumeAttackBoost(TrainingBattleManager battleManager, int attackBoostStack)
    {
        if (battleManager?.playerData == null || attackBoostStack <= 0)
        {
            return;
        }

        battleManager.playerData.ConsumeBuffStack(BattleRuntimeDefinitions.AttackBoostBuffId, attackBoostStack);
    }

    private void ResolveAttackAmount(TrainingBattleManager battleManager, int forwardedAmount, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = amount > 0 ? amount : Mathf.Max(0, forwardedAmount);
        int formulaBaseValue = forwardedAmount > 0 ? forwardedAmount : amount;

        if (string.IsNullOrWhiteSpace(amountFormula))
        {
            return;
        }

        if (TryParseMultiplierFormula(amountFormula, out float parsedMultiplier))
        {
            cardMultiplier = parsedMultiplier;
            return;
        }

        baseAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList, formulaBaseValue);
    }

    private bool TryParseMultiplierFormula(string formula, out float multiplier)
    {
        multiplier = 1f;
        if (string.IsNullOrWhiteSpace(formula))
            return false;

        string trimmed = formula.Trim();
        if (!trimmed.StartsWith("*"))
            return false;

        string numeric = trimmed.Substring(1);
        return float.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out multiplier);
    }
}
