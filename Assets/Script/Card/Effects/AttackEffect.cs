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
        int finalAmount = BuildFinalDamageAmount(battleManager);
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

                int targetBarrierBefore = targetMonster.defense;
                totalDamageDealt += targetMonster.TakeDamage(finalAmount, 0);
                battleManager.HandlePlayerAttackResolved(targetMonster, targetBarrierBefore, targetMonster.defense);
                break;

            case TargetType.Self:
                if (battleManager.playerData == null)
                {
                    Debug.LogWarning("[AttackEffect] PlayerData is missing. Self target effect cancelled.");
                    return;
                }

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

    private int BuildFinalDamageAmount(TrainingBattleManager battleManager)
    {
        Card sourceCard = battleManager?.battleContext?.GetContextCard("ThisCard")
            ?? battleManager?.battleContext?.GetLastPlayedCard();
        ResolveAttackAmount(battleManager, out int baseAmount, out float cardMultiplier);
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

    private void ResolveAttackAmount(TrainingBattleManager battleManager, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = amount;

        if (string.IsNullOrWhiteSpace(amountFormula))
        {
            return;
        }

        if (TryParseMultiplierFormula(amountFormula, out float parsedMultiplier))
        {
            cardMultiplier = parsedMultiplier;
            return;
        }

        baseAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList, amount);
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
