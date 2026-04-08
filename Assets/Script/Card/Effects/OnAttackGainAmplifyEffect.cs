using UnityEngine;

[System.Serializable]
public class OnAttackGainAmplifyEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null)
        {
            return;
        }

        int amplifyAmount = ResolveAmount(battleManager, forwardedAmount);
        if (amplifyAmount <= 0)
        {
            return;
        }

        battleManager.RegisterAttackGainAmplifyThisTurn(amplifyAmount);
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
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
