using UnityEngine;
using System.Collections.Generic;

public class SelectCardEffect : ICardEffect
{
    public int count;
    public TargetType target; // Where to select from (Hand, Deck, Discard, etc.)

    public void Execute(TrainingBattleManager battleManager)
    {
         Debug.Log($"[SelectCard] Triggered selection of {count} cards from {target}. (UI implementation pending)");
    }
}
