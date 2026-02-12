using UnityEngine;

public class BarrierEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = amount;
        
        if (!string.IsNullOrEmpty(amountFormula))
        {
            finalAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }

        // Barrier targets the player (Self)
        if (battleManager.playerData != null)
        {
            battleManager.playerData.AddDefense(finalAmount);
            battleManager.UpdateAllUI();
        }
    }
}
