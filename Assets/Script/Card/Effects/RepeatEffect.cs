using UnityEngine;

public class RepeatEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public ICardEffect effectToRepeat;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        ExecuteInternal(battleManager, amount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null || effectToRepeat == null) {
            return;
        }

        int repeatCount = ResolveRepeatCount(battleManager, forwardedAmount);
        for (int i = 0; i < repeatCount; i++) {
            effectToRepeat.Execute(battleManager, forwardedAmount);
        }
    }

    private int ResolveRepeatCount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula)) {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                amountFormula,
                battleManager.battleContext,
                battleManager.playerData,
                forwardedAmount));
        }

        if (amount > 0) {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
