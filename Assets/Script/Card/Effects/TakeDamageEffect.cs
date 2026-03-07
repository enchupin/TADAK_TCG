using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TakeDamageEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, amount);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        ExecuteInternal(battleManager, amount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null || battleManager.playerData == null) {
            Debug.LogWarning("[TakeDamageEffect] PlayerData가 없어 효과를 실행할 수 없습니다");
            return;
        }

        int finalAmount = ResolveDamageAmount(battleManager, forwardedAmount);
        if (finalAmount <= 0) {
            return;
        }

        battleManager.playerData.TakeDamage(finalAmount);
        battleManager.UpdateAllUI();
    }

    private int ResolveDamageAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        int baseAmount = amount > 0 ? amount : Mathf.Max(0, forwardedAmount);
        int formulaBaseValue = forwardedAmount > 0 ? forwardedAmount : amount;

        if (!string.IsNullOrWhiteSpace(amountFormula)) {
            if (IsTargetHpFormula(amountFormula)) {
                Monster targetMonster = ResolveCurrentTarget(battleManager);
                return targetMonster != null ? Mathf.Max(0, targetMonster.hp) : 0;
            }

            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                amountFormula,
                battleManager.battleContext,
                battleManager.playerData,
                formulaBaseValue));
        }

        return Mathf.Max(0, baseAmount);
    }

    private static bool IsTargetHpFormula(string formula)
    {
        return string.Equals(formula, "Target.Hp", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(formula, "TargetHp", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Monster ResolveCurrentTarget(TrainingBattleManager battleManager)
    {
        if (battleManager.currentTarget != null && !battleManager.currentTarget.IsDead()) {
            return battleManager.currentTarget;
        }

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters != null && livingMonsters.Count == 1) {
            return livingMonsters[0];
        }

        return null;
    }
}
