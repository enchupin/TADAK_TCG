using UnityEngine;
using System.Collections.Generic;

public class BarrierEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;
    public List<ICardEffect> onActions;

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
        int finalAmount = ResolveBarrierAmount(battleManager, forwardedAmount);

        // Barrier only applies to player when target is Self.
        if (target == TargetType.Self && battleManager.playerData != null)
        {
            battleManager.playerData.AddDefense(finalAmount);
            battleManager.UpdateAllUI();
        }

        if (onActions != null) {
            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, finalAmount);
            }
        }
    }

    private int ResolveBarrierAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
