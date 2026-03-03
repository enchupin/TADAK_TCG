using UnityEngine;
using System.Collections.Generic;

public class SelectCardEffect : ICardEffect
{
    public int count;
    public MoveZoneType from;
    public List<int> cardIdFilter;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, count);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        int resolvedCount = amount >= 0 ? amount : count;
        ExecuteInternal(battleManager, resolvedCount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int requestCount)
    {
        if (battleManager == null || battleManager.battleContext == null || battleManager.usableDeckManager == null || battleManager.handManager == null) {
            return;
        }

        List<Card> sourceCards = ResolveSourceCards(battleManager);
        if (sourceCards.Count == 0) {
            battleManager.battleContext.SetSelectedCards(new List<Card>());
            return;
        }

        int selectCount = Mathf.Clamp(requestCount, 0, sourceCards.Count);
        if (battleManager.OpenSelectCardPanel(sourceCards, selectCount, selectedCards => ApplySelectionResult(battleManager, selectedCards))) {
            return;
        }

        List<Card> fallbackCards = new List<Card>();
        for (int i = 0; i < selectCount; i++) {
            Card selected = sourceCards[i];
            if (selected != null) {
                fallbackCards.Add(selected);
            }
        }

        ApplySelectionResult(battleManager, fallbackCards);
    }

    private void ApplySelectionResult(TrainingBattleManager battleManager, List<Card> selectedCards)
    {
        List<Card> safeSelectedCards = selectedCards ?? new List<Card>();
        battleManager.battleContext.SetSelectedCards(safeSelectedCards);
        Debug.Log($"[SelectCard] {from}에서 {safeSelectedCards.Count}장 선택");

        if (safeSelectedCards.Count == 0 || onActions == null) {
            return;
        }

        foreach (ICardEffect onAction in onActions) {
            onAction?.Execute(battleManager, safeSelectedCards.Count);
        }
    }

    private List<Card> ResolveSourceCards(TrainingBattleManager battleManager)
    {
        List<Card> sourceCards = new List<Card>();

        switch (from) {
            case MoveZoneType.Hand:
                AddHandCards(sourceCards, battleManager);
                break;
            case MoveZoneType.DrawPile:
                AddUnique(sourceCards, battleManager.usableDeckManager.GetDrawPile());
                break;
            case MoveZoneType.DiscardPile:
                AddUnique(sourceCards, battleManager.usableDeckManager.GetDiscardPile());
                break;
            case MoveZoneType.AllCards:
                AddAllCards(sourceCards);
                break;
            case MoveZoneType.CardId:
                AddCardsByIdFilter(sourceCards);
                break;
            case MoveZoneType.Source:
            case MoveZoneType.None:
                AddUnique(sourceCards, battleManager.battleContext.GetSelectedCards());
                break;
        }

        if (cardIdFilter != null && cardIdFilter.Count > 0) {
            sourceCards = sourceCards.FindAll(card => card != null && cardIdFilter.Contains(card.cardId));
        }

        return sourceCards;
    }

    private static void AddHandCards(List<Card> target, TrainingBattleManager battleManager)
    {
        List<Card> handCards = battleManager.handManager.GetHandCards();
        Card currentPlayedCard = battleManager.battleContext.GetLastPlayedCard();
        foreach (Card card in handCards) {
            if (card != null && card != currentPlayedCard) {
                AddUnique(target, card);
            }
        }
    }

    private static void AddAllCards(List<Card> target)
    {
        List<CardData> allCardData = CardManager.GetAllCards();
        if (allCardData == null) {
            return;
        }

        foreach (CardData cardData in allCardData) {
            if (cardData == null) {
                continue;
            }

            Card card = cardData.ToCard();
            if (card != null) {
                AddUnique(target, card);
            }
        }
    }

    private void AddCardsByIdFilter(List<Card> target)
    {
        if (cardIdFilter == null || cardIdFilter.Count == 0) {
            return;
        }

        foreach (int cardId in cardIdFilter) {
            CardData cardData = CardManager.GetCard(cardId);
            if (cardData == null) {
                continue;
            }

            Card card = cardData.ToCard();
            if (card != null) {
                AddUnique(target, card);
            }
        }
    }

    private static void AddUnique(List<Card> target, List<Card> source)
    {
        if (target == null || source == null) {
            return;
        }

        foreach (Card card in source) {
            AddUnique(target, card);
        }
    }

    private static void AddUnique(List<Card> target, Card card)
    {
        if (target == null || card == null) {
            return;
        }

        if (!target.Contains(card)) {
            target.Add(card);
        }
    }
}
