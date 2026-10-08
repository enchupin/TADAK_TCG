using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class SelectCardEffect : ICardEffect
{
    public int count;
    public MoveZoneType from;
    public List<int> cardIdFilter;
    public string characterFilter;
    public bool random;
    public bool allowFewerSelection;
    public bool upgradeableOnly;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager), battleManager));
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        int resolvedCount = amount >= 0 ? amount : count;
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager, resolvedCount), battleManager));
    }

    public IEnumerator ExecuteSequence(TrainingBattleManager battleManager, int? amount = null)
    {
        if (battleManager == null || battleManager.battleContext == null || battleManager.usableDeckManager == null || battleManager.handManager == null)
            yield break;

        List<Card> sourceCards = ResolveSourceCards(battleManager);
        int requestCount = amount.HasValue && amount.Value >= 0 ? amount.Value : count;
        int selectCount = Mathf.Clamp(requestCount, 0, sourceCards.Count);
        List<Card> result = new List<Card>();
        if (selectCount > 0)
        {
            if (random)
            {
                result = PickRandomCards(sourceCards, selectCount);
            }
            else
            {
                bool confirmed = false;
                bool opened = battleManager.OpenSelectCardPanel(sourceCards, selectCount, cards =>
                {
                    result = cards ?? new List<Card>();
                    confirmed = true;
                }, allowFewerSelection);
                if (opened)
                {
                    while (!confirmed)
                        yield return null;
                }
                else
                {
                    result = sourceCards.GetRange(0, selectCount);
                }
            }
        }

        yield return ApplySelectionResult(battleManager, result);
    }

    private static List<Card> PickRandomCards(List<Card> sourceCards, int selectCount)
    {
        List<Card> candidates = sourceCards != null ? new List<Card>(sourceCards) : new List<Card>();
        List<Card> selectedCards = new List<Card>();
        int resolvedCount = Mathf.Clamp(selectCount, 0, candidates.Count);
        for (int i = 0; i < resolvedCount; i++) {
            int index = Random.Range(0, candidates.Count);
            selectedCards.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return selectedCards;
    }

    private IEnumerator ApplySelectionResult(TrainingBattleManager battleManager, List<Card> selectedCards)
    {
        BattleContext context = battleManager.battleContext;
        List<Card> previousSelected = context.GetSelectedCards();
        List<Card> previousContext = context.GetContextCards("Selected");
        List<Card> previousCardContext = context.GetContextCards("SelectedCard");
        List<Card> safeSelectedCards = selectedCards ?? new List<Card>();
        context.SetSelectedCards(safeSelectedCards);
        context.SetContextCards("Selected", safeSelectedCards);
        context.SetContextCards("SelectedCard", safeSelectedCards);
        Debug.Log($"[SelectCard] {from}에서 {safeSelectedCards.Count}장 선택");
        try
        {
            if (safeSelectedCards.Count > 0)
                yield return CardEffectSequence.Execute(onActions, battleManager, safeSelectedCards.Count);
        }
        finally
        {
            context.SetSelectedCards(previousSelected);
            context.SetContextCards("Selected", previousContext);
            context.SetContextCards("SelectedCard", previousCardContext);
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

        Character? filteredCharacter = ResolveCharacterFilter(battleManager);
        if (filteredCharacter.HasValue) {
            sourceCards = sourceCards.FindAll(card => card != null && card.character == filteredCharacter.Value);
        }

        if (upgradeableOnly) {
            sourceCards = sourceCards.FindAll(IsUpgradeableCard);
        }

        return sourceCards;
    }

    private static bool IsUpgradeableCard(Card card)
    {
        return card != null
            && Mathf.Abs(card.cardId) % 10 == 0
            && card.enforceCardIds != null
            && card.enforceCardIds.Count > 0;
    }

    private Character? ResolveCharacterFilter(TrainingBattleManager battleManager)
    {
        if (string.IsNullOrWhiteSpace(characterFilter)) {
            return null;
        }

        if (string.Equals(characterFilter, "Self", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(characterFilter, "Source", System.StringComparison.OrdinalIgnoreCase)) {
            return ResolveSourceCharacter(battleManager);
        }

        if (int.TryParse(characterFilter, out int characterId) && System.Enum.IsDefined(typeof(Character), characterId)) {
            return (Character)characterId;
        }

        if (System.Enum.TryParse(characterFilter, true, out Character parsedCharacter)) {
            return parsedCharacter;
        }

        Debug.LogWarning($"[SelectCard] 알 수 없는 characterFilter입니다: {characterFilter}");
        return null;
    }

    private static void AddHandCards(List<Card> target, TrainingBattleManager battleManager)
    {
        List<Card> handCards = battleManager.handManager.GetHandCards();
        Card currentPlayedCard = battleManager.battleContext.GetContextCard("ThisCard")
            ?? battleManager.battleContext.GetLastPlayedCard();
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

        Card sourceCard = battleManager?.battleContext?.GetContextCard("ThisCard")
            ?? battleManager?.battleContext?.GetLastPlayedCard();
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

        Card lastPlayedCard = battleManager.battleContext.GetContextCard("ThisCard")
            ?? battleManager.battleContext.GetLastPlayedCard();
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
