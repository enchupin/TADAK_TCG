using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReduceCostEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public string durationText;
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

        List<Card> targetCards = CardEffectRuntimeUtility.ResolveCards(battleManager, null, subject);
        if (targetCards.Count == 0) {
            return;
        }

        int reduceAmount = Mathf.Abs(CardEffectRuntimeUtility.ResolveCardValueAmount(battleManager, amount, amountFormula, forwardedAmount));
        bool turnOnly = string.Equals(durationText, "Turn", System.StringComparison.OrdinalIgnoreCase);

        foreach (Card card in targetCards) {
            card?.ApplyCostModifier(-reduceAmount, turnOnly);
        }

        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, targetCards);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
