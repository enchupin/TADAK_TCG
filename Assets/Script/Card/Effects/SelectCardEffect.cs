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
            battleManager.battleContext.ClearContextCards("Selected");
            battleManager.battleContext.ClearContextCards("SelectedCard");
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
        battleManager.battleContext.SetContextCards("Selected", safeSelectedCards);
        battleManager.battleContext.SetContextCards("SelectedCard", safeSelectedCards);
        Debug.Log($"[SelectCard] {from}에서 {safeSelectedCards.Count}장 선택");

        if (safeSelectedCards.Count == 0 || onActions == null) {
            battleManager.battleContext.ClearContextCards("Selected");
            battleManager.battleContext.ClearContextCards("SelectedCard");
            battleManager.battleContext.SetSelectedCards(new List<Card>());
            return;
        }

        foreach (ICardEffect onAction in onActions) {
            onAction?.Execute(battleManager, safeSelectedCards.Count);
        }

        battleManager.battleContext.ClearContextCards("Selected");
        battleManager.battleContext.ClearContextCards("SelectedCard");
        battleManager.battleContext.SetSelectedCards(new List<Card>());
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
                AddAllBattleCards(sourceCards, battleManager);
                break;
            case MoveZoneType.CardId:
                AddCardsByIdFilter(sourceCards);
                break;
            case MoveZoneType.Basic:
                AddCardsByCardType(sourceCards, battleManager, true);
                break;
            case MoveZoneType.Unique:
                AddCardsByCardType(sourceCards, battleManager, false);
                break;
            case MoveZoneType.AllUnique:
                AddAllUniqueCards(sourceCards);
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

    private static void AddAllBattleCards(List<Card> target, TrainingBattleManager battleManager)
    {
        if (battleManager == null || battleManager.usableDeckManager == null || battleManager.handManager == null) {
            return;
        }

        AddHandCards(target, battleManager);
        AddUnique(target, battleManager.usableDeckManager.GetDiscardPile());
        AddUnique(target, battleManager.usableDeckManager.GetDrawPile());
    }

    private static void AddCardsByCardType(List<Card> target, TrainingBattleManager battleManager, bool isBasicTarget)
    {
        Character? sourceCharacter = ResolveSourceCharacter(battleManager);
        if (!sourceCharacter.HasValue) {
            Debug.LogWarning("[SelectCard] Basic/Unique 기준 캐릭터를 찾을 수 없습니다");
            return;
        }

        Card sourceCard = battleManager?.battleContext?.GetLastPlayedCard();
        int excludedUniqueCardId = ResolveExcludedUniqueCardId(sourceCard);

        List<CardData> characterCards = CardManager.GetCardsByCharacter(sourceCharacter.Value);
        if (characterCards == null) {
            return;
        }

        foreach (CardData cardData in characterCards) {
            if (cardData == null) {
                continue;
            }

            bool isBasic = IsBasicCardId(cardData.cardId);
            if (isBasicTarget) {
                if (!isBasic) {
                    continue;
                }
            } else {
                if (!IsUniqueCardId(cardData.cardId)) {
                    continue;
                }

                if (excludedUniqueCardId > 0 && cardData.cardId == excludedUniqueCardId) {
                    continue;
                }
            }

            Card card = cardData.ToCard();
            if (card != null) {
                AddUnique(target, card);
            }
        }
    }

    private static void AddAllUniqueCards(List<Card> target)
    {
        List<CardData> allCards = CardManager.GetAllCards();
        if (allCards == null) {
            return;
        }

        foreach (CardData cardData in allCards) {
            if (cardData == null || cardData.character == Character.Monster || !IsUniqueCardId(cardData.cardId)) {
                continue;
            }

            Card card = cardData.ToCard();
            if (card != null) {
                AddUnique(target, card);
            }
        }
    }

    private static Character? ResolveSourceCharacter(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null) {
            return null;
        }

        Card lastPlayedCard = battleManager.battleContext.GetLastPlayedCard();
        if (lastPlayedCard == null) {
            return null;
        }

        return lastPlayedCard.character;
    }

    private static int ResolveExcludedUniqueCardId(Card sourceCard)
    {
        if (sourceCard == null) {
            return 0;
        }

        int familyBaseId = ResolveCardFamilyBaseId(sourceCard.cardId);
        if (familyBaseId != 302070 && familyBaseId != 102040) {
            return 0;
        }

        return familyBaseId;
    }

    private static int ResolveCardFamilyBaseId(int cardId)
    {
        int suffix = Mathf.Abs(cardId) % 10;
        return suffix == 0 ? cardId : cardId - suffix;
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

    private static bool IsBasicCardId(int cardId)
    {
        int suffix = Mathf.Abs(cardId) % 1000;
        return suffix == 10 || suffix == 20;
    }

    private static bool IsUniqueCardId(int cardId)
    {
        if (IsBasicCardId(cardId)) {
            return false;
        }

        return Mathf.Abs(cardId) % 10 == 0;
    }
}
