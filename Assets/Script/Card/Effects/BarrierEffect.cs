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
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

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
}
