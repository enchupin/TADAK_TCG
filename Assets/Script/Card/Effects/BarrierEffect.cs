using UnityEngine;

public class BarrierEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;
    public ICardEffect onAction;

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

        onAction?.Execute(battleManager, finalAmount);
    }
}
