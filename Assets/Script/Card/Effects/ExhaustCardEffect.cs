using System;
using System.Collections.Generic;
using UnityEngine;

public class ExhaustCardEffect : ICardEffect
{
    public MoveZoneType from;
    public string subject;
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null || battleManager.usableDeckManager == null || battleManager.handManager == null)
        {
            return;
        }

        List<Card> sourceCards = ResolveSourceCards(battleManager);
        if (sourceCards.Count == 0)
        {
            return;
        }

        List<Card> cardsToExhaust = ResolveCardsToExhaust(sourceCards, battleManager);
        if (cardsToExhaust.Count == 0)
        {
            return;
        }

        int exhaustedCount = 0;
        foreach (Card card in cardsToExhaust)
        {
            if (card == null)
            {
                continue;
            }

            if (!DetachFromSource(card, battleManager))
            {
                continue;
            }

            exhaustedCount++;
        }

        if (exhaustedCount <= 0)
        {
            return;
        }

        battleManager.battleContext?.OnCardsExhausted(exhaustedCount);

        if (onActions != null)
        {
            foreach (ICardEffect onAction in onActions)
            {
                onAction?.Execute(battleManager, exhaustedCount);
            }
        }

        battleManager.UpdateAllUI();
    }

    private List<Card> ResolveSourceCards(TrainingBattleManager battleManager)
    {
        MoveZoneType resolvedFrom = from == MoveZoneType.None ? MoveZoneType.Hand : from;
        List<Card> sourceCards = new List<Card>();

        switch (resolvedFrom)
        {
            case MoveZoneType.Hand:
                List<Card> handCards = battleManager.handManager.GetHandCards();
                Card currentPlayedCard = battleManager.battleContext?.GetLastPlayedCard();
                foreach (Card handCard in handCards)
                {
                    if (handCard != null && handCard != currentPlayedCard)
                    {
                        sourceCards.Add(handCard);
                    }
                }
                break;
            case MoveZoneType.DrawPile:
                sourceCards.AddRange(battleManager.usableDeckManager.GetDrawPile());
                break;
            case MoveZoneType.DiscardPile:
                sourceCards.AddRange(battleManager.usableDeckManager.GetDiscardPile());
                break;
            case MoveZoneType.Source:
                AddUnique(sourceCards, battleManager.battleContext?.GetSelectedCards());
                break;
        }

        return sourceCards;
    }

    private List<Card> ResolveCardsToExhaust(List<Card> sourceCards, TrainingBattleManager battleManager)
    {
        if (IsAllFormula())
        {
            return new List<Card>(sourceCards);
        }

        if (IsSelectedSubject())
        {
            List<Card> selectedCards = battleManager.battleContext?.GetSelectedCards() ?? new List<Card>();
            List<Card> result = new List<Card>();
            foreach (Card selected in selectedCards)
            {
                if (selected != null && sourceCards.Contains(selected) && !result.Contains(selected))
                {
                    result.Add(selected);
                }
            }
            return result;
        }

        int exhaustCount = ResolveExhaustCount(sourceCards.Count, battleManager);
        List<Card> resultCards = new List<Card>();
        for (int i = 0; i < exhaustCount && i < sourceCards.Count; i++)
        {
            resultCards.Add(sourceCards[i]);
        }
        return resultCards;
    }

    private int ResolveExhaustCount(int sourceCount, TrainingBattleManager battleManager)
    {
        if (sourceCount <= 0)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(amountFormula) && !string.Equals(amountFormula, "all", StringComparison.OrdinalIgnoreCase))
        {
            int evaluated = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
            return Mathf.Clamp(evaluated, 0, sourceCount);
        }

        if (amount > 0)
        {
            return Mathf.Clamp(amount, 0, sourceCount);
        }

        return 1;
    }

    private bool DetachFromSource(Card card, TrainingBattleManager battleManager)
    {
        MoveZoneType resolvedFrom = from == MoveZoneType.None ? MoveZoneType.Hand : from;

        switch (resolvedFrom)
        {
            case MoveZoneType.Hand:
                return battleManager.handManager.RemoveCard(card);
            case MoveZoneType.DrawPile:
                return battleManager.usableDeckManager.RemoveFromDrawPile(card);
            case MoveZoneType.DiscardPile:
                if (battleManager.usableDeckManager.GetDiscardPile().Contains(card))
                {
                    battleManager.usableDeckManager.RemoveFromDiscard(card);
                    return true;
                }
                return false;
            case MoveZoneType.Source:
                return RemoveFromAnyZone(card, battleManager);
            default:
                return false;
        }
    }

    private static bool RemoveFromAnyZone(Card card, TrainingBattleManager battleManager)
    {
        if (battleManager.handManager.RemoveCard(card))
        {
            return true;
        }

        if (battleManager.usableDeckManager.RemoveFromDrawPile(card))
        {
            return true;
        }

        if (battleManager.usableDeckManager.GetDiscardPile().Contains(card))
        {
            battleManager.usableDeckManager.RemoveFromDiscard(card);
            return true;
        }

        return false;
    }

    private bool IsSelectedSubject()
    {
        return string.Equals(subject, "Selected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subject, "SelectedCard", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAllFormula()
    {
        return string.Equals(amountFormula, "all", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddUnique(List<Card> target, List<Card> source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (Card card in source)
        {
            if (card != null && !target.Contains(card))
            {
                target.Add(card);
            }
        }
    }
}
