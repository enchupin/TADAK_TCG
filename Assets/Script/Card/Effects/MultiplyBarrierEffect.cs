using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방어도 배율 이펙트
/// </summary>
[System.Serializable]
public class MultiplyBarrierEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.Self;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        ExecuteInternal(battleManager, forwardedAmount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null)
        {
            return;
        }

        int multiplier = ResolveMultiplier(battleManager, forwardedAmount);
        if (multiplier < 0)
        {
            multiplier = 0;
        }

        if (target == TargetType.Self && battleManager.playerData != null)
        {
            battleManager.playerData.defense = Mathf.Max(0, battleManager.playerData.defense * multiplier);
            battleManager.UpdateAllUI();
            return;
        }

        List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
        foreach (Monster monster in targets)
        {
            monster.defense = Mathf.Max(0, monster.defense * multiplier);
            monster.UpdateUI();
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveMultiplier(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount);
        }

        if (amount > 0)
        {
            return amount;
        }

        return forwardedAmount > 0 ? forwardedAmount : 1;
    }
}
