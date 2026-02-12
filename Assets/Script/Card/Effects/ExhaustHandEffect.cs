using UnityEngine;
using System.Collections.Generic;

public class ExhaustHandEffect : ICardEffect
{
    public int count;
    public bool isRandom;
    public TargetType target;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager.handManager == null) return;

        List<Card> hand = battleManager.handManager.GetHandCards();
        int exhaustCount = Mathf.Min(count, hand.Count);

        // TODO: Support choice. For now, random or first available.
        for (int i = 0; i < exhaustCount; i++)
        {
            if (hand.Count == 0) break;
            
            int idx = Random.Range(0, hand.Count);
            Card card = hand[idx];
            
            // Exhaust logic: Move to Exhaust pile (not yet implemented in DeckManager, so just remove from hand and don't add to discard)
            // Ideally, UsableDeckManager should have AddToExhaust
            
            battleManager.handManager.RemoveCardFromHand(battleManager.handManager.GetCardUI(card));
            
            if (battleManager.usableDeckManager != null)
            {
                // battleManager.usableDeckManager.AddToExhaust(card); // TODO: Implement this
                Debug.Log($"[ExhaustHandEffect] Exhausted card: {card.cardName}");
            }
        }
        
        if (count >= 99) // Exhaust Hand
        {
            battleManager.handManager.ClearHand();
        }
        
        battleManager.UpdateAllUI();
    }
}
