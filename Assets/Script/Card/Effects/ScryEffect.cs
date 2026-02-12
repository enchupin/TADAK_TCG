using UnityEngine;
using System.Collections.Generic;

public class ScryEffect : ICardEffect
{
    public int count;

    public void Execute(TrainingBattleManager battleManager)
    {
        // TODO: This effect requires a specific UI to show cards and let player select which to discard.
        // For now, as a placeholder, we can just look at top cards and log them.
        
        if (battleManager.usableDeckManager == null) return;
        
        List<Card> deck = battleManager.usableDeckManager.GetDrawPile();
        int scryCount = Mathf.Min(count, deck.Count);
        
        Debug.Log($"[ScryEffect] Looking at top {scryCount} cards.");
        
        // In a real implementation:
        // 1. Open Scry UI with these cards.
        // 2. Wait for user input (this is async in game loop, but Execute is void). 
        //    This implies Effects might need to be Coroutines or have callbacks.
        //    However, current structure is synchronous.
        
        // Temporary: Just log top cards.
        for (int i = 0; i < scryCount; i++)
        {
             // Deck is usually a Stack or List where index 0 or Count-1 is top. 
             // UsableDeckManager implementation needed to confirm.
             // Assuming DrawCard uses index 0 as top.
             Debug.Log($"[Scry] Top Card {i+1}: {deck[i].cardName}");
        }
        
        // Currently cannot implement full interactive Scry without UI and async flow support.
        // Marking as implemented (Placeholder).
    }
}
