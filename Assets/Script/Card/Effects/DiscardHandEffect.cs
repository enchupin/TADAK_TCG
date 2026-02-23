using UnityEngine;
using System.Collections.Generic;

public class DiscardHandEffect : ICardEffect
{
    public int count;
    public string amountFormula;
    public bool isRandom;
    public TargetType target; // e.g., Self (discard own hand)

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager.handManager == null || battleManager.usableDeckManager == null) return;

        // "all" means move every card currently in hand to discard pile.
        if (!string.IsNullOrEmpty(amountFormula) && amountFormula.Trim().ToLower() == "all")
        {
            List<Card> allHandCards = battleManager.handManager.ClearHand();
            battleManager.usableDeckManager.AddToDiscard(allHandCards);
            battleManager.UpdateAllUI();
            return;
        }

        List<Card> workingHand = battleManager.handManager.GetHandCards();
        int discardCount = Mathf.Min(count, workingHand.Count);

        for (int i = 0; i < discardCount; i++)
        {
            if (workingHand.Count == 0) break;

            int idx = isRandom ? Random.Range(0, workingHand.Count) : 0;
            Card card = workingHand[idx];
            workingHand.RemoveAt(idx);

            CardUI cardUI = battleManager.handManager.GetCardUI(card);
            if (cardUI == null) continue;

            battleManager.usableDeckManager.AddToDiscard(card);
            battleManager.handManager.RemoveCardFromHand(cardUI);
        }

        battleManager.UpdateAllUI();
    }
}
