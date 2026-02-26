using UnityEngine;

public class ExhaustHandEffect : ICardEffect
{
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager.handManager == null) return;

        if (!string.IsNullOrEmpty(amountFormula) && amountFormula.Trim().ToLower() == "all")
        {
            // Exhaust all cards in hand: remove from hand without moving to discard pile.
            int exhaustedCount = battleManager.handManager.GetHandCards().Count;
            battleManager.handManager.ClearHand();
            battleManager.battleContext?.OnCardsExhausted(exhaustedCount);
            battleManager.UpdateAllUI();
        }
    }
}
