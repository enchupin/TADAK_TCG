using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DrawUntilHandFullEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager?.playerData == null || battleManager.handManager == null)
        {
            return;
        }

        int hpLossPerDraw = ResolveAmount(battleManager, forwardedAmount);
        int guardCount = 0;
        while (battleManager.handManager.GetHandCount() < battleManager.MaxHandCardCount)
        {
            if (guardCount++ > 100)
            {
                break;
            }

            if (!battleManager.CanDrawCards())
            {
                break;
            }

            if (battleManager.usableDeckManager != null
                && battleManager.usableDeckManager.GetRemainingCardCount() <= 0
                && battleManager.usableDeckManager.GetDiscardPileCount() <= 0)
            {
                break;
            }

            if (hpLossPerDraw > 0)
            {
                int lostHp = battleManager.playerData.LoseHp(hpLossPerDraw);
                if (lostHp <= 0 || battleManager.playerData.IsDead())
                {
                    break;
                }
            }

            int handCountBeforeDraw = battleManager.handManager.GetHandCount();
            List<Card> drawnCards = battleManager.DrawCardsAndGet(1);
            if (drawnCards == null || drawnCards.Count == 0)
            {
                break;
            }

            if (battleManager.handManager.GetHandCount() <= handCountBeforeDraw)
            {
                break;
            }
        }
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager?.battleContext, battleManager?.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
