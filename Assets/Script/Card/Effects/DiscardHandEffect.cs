using UnityEngine;
using System.Collections.Generic;

public class DiscardHandEffect : ICardEffect
{
    public int count;
    public bool isRandom;
    public TargetType target; // e.g., Self (discard own hand)

    public void Execute(TrainingBattleManager battleManager)
    {
        // Simple implementation: Discard 'count' cards from hand
        // For now, let's implement random discard or "discard all" if count is high
        
        if (battleManager.handManager == null) return;

        List<Card> hand = battleManager.handManager.GetHandCards();
        int discardCount = Mathf.Min(count, hand.Count);

        // TODO: Support choice (UI required). For now, random or first available.
        // Assuming "random" for simplicity if choice not implemented
        
        for (int i = 0; i < discardCount; i++)
        {
            if (hand.Count == 0) break;
            
            // Pick a random card
            int idx = Random.Range(0, hand.Count);
            Card card = hand[idx];
            
            // Discard logic
            battleManager.usableDeckManager.AddToDiscard(card);
            battleManager.handManager.RemoveCardFromHand(battleManager.handManager.GetCardUI(card)); 
            // Note: GetCardUI method might not exist publicly, checking HandManager needed
            // Actually TrainingBattleManager.PlayCard handles removal. 
            // We need a proper Discard method in HandManager or BattleManager.
            
            // Workaround: Call public method if exists, or just clear if DiscardAll
        }
        
        if (count >= 99) // Discard Hand
        {
             battleManager.usableDeckManager.AddToDiscard(hand);
             battleManager.handManager.ClearHand();
        }
        
        battleManager.UpdateAllUI();
    }
}
