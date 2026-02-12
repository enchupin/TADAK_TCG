using UnityEngine;
using System.Collections.Generic;

public class ChoiceHandEffect : ICardEffect
{
    public int count;
    // public List<ICardEffect> choiceEffects; // Effect to apply on selected card?

    public void Execute(TrainingBattleManager battleManager)
    {
        // Similar to Scry, requires UI to select cards from Hand.
        // e.g. "Select 1 card to Discard", "Select 1 card to Upgrade"
        
        Debug.Log($"[ChoiceHand] Triggered choice for {count} cards. (UI implementation pending)");
        
        // TODO: Integrate with Selection UI Manager when available.
    }
}
