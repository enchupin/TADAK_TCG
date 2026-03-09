using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CostEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public string subject;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null) {
            return;
        }

        List<Card> targetCards = CardEffectRuntimeUtility.ResolveCards(battleManager, "ThisCard", subject);
        if (targetCards.Count == 0) {
            return;
        }

        int costDelta = CardEffectRuntimeUtility.ResolveCardValueAmount(battleManager, amount, amountFormula, forwardedAmount);
        foreach (Card card in targetCards) {
            card?.ApplyCostModifier(costDelta, false);
        }

        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, targetCards);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
