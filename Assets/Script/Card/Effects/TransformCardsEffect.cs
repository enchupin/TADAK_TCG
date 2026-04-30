using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransformCardsEffect : ICardEffect
{
    public MoveZoneType from = MoveZoneType.Source;
    public string subject;
    public string characterFilter;
    public string cardId;
    public List<int> cardIdList;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        if (!int.TryParse(cardId, out int templateCardId) || templateCardId <= 0)
        {
            Debug.LogWarning($"[TransformCardsEffect] 변경할 카드 ID가 올바르지 않습니다: {cardId}");
            return;
        }

        Card templateCard = CardManager.GetCardAsCard(templateCardId);
        if (templateCard == null)
        {
            Debug.LogWarning($"[TransformCardsEffect] 변경할 템플릿 카드를 찾을 수 없습니다: {templateCardId}");
            return;
        }

        List<Card> cards = ResolveTargetCards(battleManager);
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        List<Card> changedCards = new List<Card>();
        foreach (Card card in cards)
        {
            if (!MatchesFilters(card))
            {
                continue;
            }

            card.ApplyTemplate(templateCard);
            battleManager.ApplyPersistentUpgradeToCard(card);
            changedCards.Add(card);
        }

        if (changedCards.Count <= 0)
        {
            return;
        }

        List<Card> changedHandCards = GetChangedHandCards(battleManager, changedCards);
        if (changedHandCards.Count > 0)
        {
            battleManager.handManager?.RefreshCardDisplays(changedHandCards);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private List<Card> ResolveTargetCards(TrainingBattleManager battleManager)
    {
        switch (from)
        {
            case MoveZoneType.Hand:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "Hand");

            case MoveZoneType.DrawPile:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "DrawPile");

            case MoveZoneType.DiscardPile:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "DiscardPile");

            case MoveZoneType.AllCards:
                List<Card> allCards = new List<Card>();
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "Hand"));
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "DrawPile"));
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "DiscardPile"));
                return allCards;

            case MoveZoneType.Source:
            case MoveZoneType.None:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, null, subject);

            default:
                Debug.LogWarning($"[TransformCardsEffect] 지원하지 않는 범위입니다: {from}");
                return new List<Card>();
        }
    }

    private bool MatchesFilters(Card card)
    {
        if (card == null)
        {
            return false;
        }

        if (cardIdList != null && cardIdList.Count > 0 && !cardIdList.Contains(card.cardId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterFilter))
        {
            return true;
        }

        return TryParseCharacter(characterFilter, out Character character)
            && card.character == character;
    }

    private static bool TryParseCharacter(string rawCharacter, out Character character)
    {
        character = default;
        if (string.IsNullOrWhiteSpace(rawCharacter))
        {
            return false;
        }

        if (Enum.TryParse(rawCharacter, true, out character))
        {
            return true;
        }

        if (int.TryParse(rawCharacter, out int characterId))
        {
            character = CharacterManager.GetCharacterEnumById(characterId);
            return true;
        }

        Debug.LogWarning($"[TransformCardsEffect] 알 수 없는 캐릭터 필터입니다: {rawCharacter}");
        return false;
    }

    private static List<Card> GetChangedHandCards(TrainingBattleManager battleManager, List<Card> changedCards)
    {
        List<Card> changedHandCards = new List<Card>();
        List<Card> handCards = battleManager?.handManager?.GetHandCards();
        if (handCards == null || changedCards == null)
        {
            return changedHandCards;
        }

        foreach (Card handCard in handCards)
        {
            if (handCard != null && changedCards.Contains(handCard))
            {
                changedHandCards.Add(handCard);
            }
        }

        return changedHandCards;
    }

    private static void AddCards(List<Card> target, List<Card> source)
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
