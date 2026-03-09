using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 드로우 효과
/// 카드를 뽑습니다.
/// </summary>
[System.Serializable]
public class DrawEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, amount);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        int finalAmount = ResolveDrawAmount(battleManager, amount);
        List<Card> drawnCards = battleManager.DrawCardsAndGet(finalAmount);
        ExecuteOnDrawnCards(battleManager, drawnCards);
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

    private void ExecuteOnDrawnCards(TrainingBattleManager battleManager, List<Card> drawnCards)
    {
        if (battleManager?.battleContext == null || onActions == null || drawnCards == null || drawnCards.Count == 0) {
            return;
        }

        foreach (Card drawnCard in drawnCards) {
            if (drawnCard == null) {
                continue;
            }

            battleManager.battleContext.SetContextCards("DrawnCard", new List<Card> { drawnCard });

            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, 1);
            }

            battleManager.battleContext.ClearContextCards("DrawnCard");
        }
    }
}
