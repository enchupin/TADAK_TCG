using UnityEngine;

/// <summary>
/// 드로우 효과
/// 카드를 뽑습니다.
/// </summary>
[System.Serializable]
public class DrawEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, amount);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        int finalAmount = ResolveDrawAmount(battleManager, amount);
        battleManager.DrawCards(finalAmount);
    }

    private int ResolveDrawAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
