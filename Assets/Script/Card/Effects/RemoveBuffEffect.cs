using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 버프 제거 이펙트
/// </summary>
[System.Serializable]
public class RemoveBuffEffect : ICardEffect
{
    public int buffId;
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
        if (battleManager == null || buffId <= 0)
        {
            return;
        }

        int removeAmount = ResolveRemoveAmount(battleManager, forwardedAmount);

        if (target == TargetType.Self)
        {
            CardEffectRuntimeUtility.RemoveBuffStacks(battleManager.playerData?.currentBuffs, buffId, removeAmount, true);
            battleManager.UpdateAllUI();
            return;
        }

        List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
        foreach (Monster monster in targets)
        {
            CardEffectRuntimeUtility.RemoveBuffStacks(monster.currentBuffs, buffId, removeAmount, true);
            monster.UpdateUI();
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveRemoveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        if (forwardedAmount > 0)
        {
            return forwardedAmount;
        }

        return 0;
    }
}
