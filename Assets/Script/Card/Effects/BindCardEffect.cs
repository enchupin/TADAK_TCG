using System.Collections.Generic;
using UnityEngine;

public class BindCardEffect : ICardEffect
{
    public int count = 1;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null || battleManager.handManager == null)
        {
            return;
        }

        Card hostCard = battleManager.battleContext.GetContextCard("ThisCard")
            ?? battleManager.battleContext.GetLastPlayedCard();
        if (hostCard == null)
        {
            return;
        }

        List<Card> candidates = ResolveCandidates(battleManager, hostCard);
        if (candidates.Count == 0)
        {
            return;
        }

        int selectCount = Mathf.Clamp(count > 0 ? count : 1, 1, candidates.Count);
        if (battleManager.OpenSelectCardPanel(candidates, selectCount, selectedCards => BindSelectedCards(battleManager, hostCard, selectedCards)))
        {
            return;
        }

        List<Card> fallbackCards = new List<Card>();
        for (int i = 0; i < selectCount; i++)
        {
            fallbackCards.Add(candidates[i]);
        }

        BindSelectedCards(battleManager, hostCard, fallbackCards);
    }

    private static List<Card> ResolveCandidates(TrainingBattleManager battleManager, Card hostCard)
    {
        List<Card> candidates = new List<Card>();
        List<Card> handCards = battleManager?.handManager?.GetHandCards();
        if (handCards == null)
        {
            return candidates;
        }

        foreach (Card handCard in handCards)
        {
            if (handCard != null && handCard != hostCard)
            {
                candidates.Add(handCard);
            }
        }

        return candidates;
    }

    private static void BindSelectedCards(TrainingBattleManager battleManager, Card hostCard, List<Card> selectedCards)
    {
        if (battleManager?.handManager == null || hostCard == null || selectedCards == null || selectedCards.Count == 0)
        {
            return;
        }

        foreach (Card selectedCard in selectedCards)
        {
            if (selectedCard == null)
            {
                continue;
            }

            if (!battleManager.handManager.RemoveCard(selectedCard))
            {
                continue;
            }

            battleManager.RemoveCardFromCombat(selectedCard);
            hostCard.AddPendingBoundCardId(selectedCard.cardId);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
