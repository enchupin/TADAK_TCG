using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 존 이동 효과
/// </summary>
[System.Serializable]
public class MoveEffect : ICardEffect
{
    public MoveZoneType from;
    public MoveZoneType to;
    public MovePositionType position;
    public string subject;
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null || battleManager.usableDeckManager == null || battleManager.handManager == null) {
            return;
        }

        List<Card> sourceCards = ResolveSourceCards(battleManager);
        if (sourceCards.Count == 0) {
            return;
        }

        List<Card> cardsToMove = ResolveCardsToMove(sourceCards, battleManager);
        if (cardsToMove.Count == 0) {
            return;
        }

        int movedCount = 0;
        foreach (Card card in cardsToMove) {
            if (card == null) {
                continue;
            }

            if (!DetachFromSource(card, battleManager)) {
                continue;
            }

            AttachToTarget(card, battleManager);
            movedCount++;
        }

        if (movedCount > 0) {
            if (to == MoveZoneType.DiscardPile) {
                battleManager.battleContext?.OnCardsDiscarded(movedCount);
            }

            if (onActions != null) {
                foreach (ICardEffect onAction in onActions) {
                    onAction?.Execute(battleManager, movedCount);
                }
            }

            battleManager.UpdateAllUI();
        }
    }

    private List<Card> ResolveSourceCards(TrainingBattleManager battleManager)
    {
        MoveZoneType resolvedFrom = from == MoveZoneType.None ? MoveZoneType.Source : from;
        List<Card> sourceCards = new List<Card>();

        switch (resolvedFrom) {
            case MoveZoneType.Hand:
                List<Card> handCards = battleManager.handManager.GetHandCards();
                Card currentPlayedCard = battleManager.battleContext?.GetLastPlayedCard();
                foreach (Card handCard in handCards) {
                    if (handCard != null && handCard != currentPlayedCard) {
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
                AddUnique(sourceCards, battleManager.battleContext?.GetContextCards(subject));
                AddUnique(sourceCards, battleManager.battleContext?.GetSelectedCards());
                break;
        }

        return sourceCards;
    }

    private List<Card> ResolveCardsToMove(List<Card> sourceCards, TrainingBattleManager battleManager)
    {
        if (IsAllFormula()) {
            return new List<Card>(sourceCards);
        }

        if (IsSelectedSubject()) {
            List<Card> selectedCards = battleManager.battleContext?.GetSelectedCards() ?? new List<Card>();
            List<Card> result = new List<Card>();
            foreach (Card selected in selectedCards) {
                if (selected != null && sourceCards.Contains(selected) && !result.Contains(selected)) {
                    result.Add(selected);
                }
            }
            return result;
        }

        int moveCount = ResolveMoveCount(sourceCards.Count, battleManager);
        List<Card> fallback = new List<Card>();
        for (int i = 0; i < moveCount && i < sourceCards.Count; i++) {
            fallback.Add(sourceCards[i]);
        }
        return fallback;
    }

    private int ResolveMoveCount(int sourceCount, TrainingBattleManager battleManager)
    {
        if (sourceCount <= 0) {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(amountFormula)) {
            int evaluated = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
            return Mathf.Clamp(evaluated, 0, sourceCount);
        }

        if (amount > 0) {
            return Mathf.Clamp(amount, 0, sourceCount);
        }

        return 1;
    }

    private bool DetachFromSource(Card card, TrainingBattleManager battleManager)
    {
        MoveZoneType resolvedFrom = from == MoveZoneType.None ? MoveZoneType.Source : from;

        switch (resolvedFrom) {
            case MoveZoneType.Hand:
                return battleManager.handManager.RemoveCard(card);
            case MoveZoneType.DrawPile:
                return battleManager.usableDeckManager.RemoveFromDrawPile(card);
            case MoveZoneType.DiscardPile:
                if (battleManager.usableDeckManager.GetDiscardPile().Contains(card)) {
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

    private bool RemoveFromAnyZone(Card card, TrainingBattleManager battleManager)
    {
        if (battleManager.handManager.RemoveCard(card)) {
            return true;
        }

        if (battleManager.usableDeckManager.RemoveFromDrawPile(card)) {
            return true;
        }

        if (battleManager.usableDeckManager.GetDiscardPile().Contains(card)) {
            battleManager.usableDeckManager.RemoveFromDiscard(card);
            return true;
        }

        return false;
    }

    private void AttachToTarget(Card card, TrainingBattleManager battleManager)
    {
        switch (to) {
            case MoveZoneType.Hand:
                battleManager.handManager.AddCard(card);
                return;
            case MoveZoneType.DiscardPile:
                battleManager.usableDeckManager.AddToDiscard(card);
                return;
            case MoveZoneType.DrawPile:
                if (position == MovePositionType.Top) {
                    battleManager.usableDeckManager.AddToDrawPileTop(card);
                }
                else {
                    battleManager.usableDeckManager.AddToDrawPileRandom(card);
                }
                return;
            default:
                Debug.LogWarning("[MoveEffect] Invalid target zone.");
                battleManager.usableDeckManager.AddToDiscard(card);
                return;
        }
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
        if (target == null || source == null) {
            return;
        }

        foreach (Card card in source) {
            if (card != null && !target.Contains(card)) {
                target.Add(card);
            }
        }
    }
}
