using System.Collections.Generic;
using UnityEngine;

// 훈련 중 저장 덱에 카드를 더하거나 덜어낼 때의 공통 규칙
// R-13 획득 선택지, R-15 캐릭터당 장수 초과 처리, R-25 유일 카드 제외를 담당
public static class TrainingDeckRewardService
{
    // 해당 캐릭터 고유 카드 기본형 중 무작위 choiceCount종을 선택지로 제시
    public static List<int> GetAcquisitionOptions(IReadOnlyList<Card> deckCards, Character character)
    {
        List<int> candidates = new List<int>();
        foreach (int cardId in CharacterManager.GetUniqueBaseCardIds(character))
        {
            CardData cardData = CardManager.GetCard(cardId);
            if (cardData == null)
            {
                continue;
            }

            // 보유 중인 유일 카드는 획득 선택지에서 제외
            bool isUnique = cardData.keywords != null && cardData.keywords.Contains(CardKeywordIds.Unique);
            if (isUnique && ContainsCardId(deckCards, cardId))
            {
                continue;
            }

            candidates.Add(cardId);
        }

        Shuffle(candidates);
        int optionCount = Mathf.Clamp(GameRules.Deck.choiceCount, 0, candidates.Count);
        return candidates.GetRange(0, optionCount);
    }

    public static List<Card> GetCharacterCards(IReadOnlyList<Card> deckCards, Character character)
    {
        List<Card> characterCards = new List<Card>();
        if (deckCards == null)
        {
            return characterCards;
        }

        foreach (Card card in deckCards)
        {
            if (card != null && card.character == character)
            {
                characterCards.Add(card);
            }
        }

        return characterCards;
    }

    // 캐릭터 카드가 상한을 넘은 장수 (0이면 제거 불필요)
    // R-46 · R-25 복제 후보: 유일 카드 제외
    public static List<Card> GetDuplicateCandidates(IReadOnlyList<Card> deckCards)
    {
        List<Card> candidates = new List<Card>();
        if (deckCards == null)
        {
            return candidates;
        }

        foreach (Card card in deckCards)
        {
            if (card != null && !card.HasKeyword(CardKeywordIds.Unique))
            {
                candidates.Add(card);
            }
        }

        return candidates;
    }

    // R-46 제거 후보: 저장 덱이 비지 않도록 2장 이상 가진 캐릭터의 카드만
    public static List<Card> GetRemovalCandidates(IReadOnlyList<Card> deckCards)
    {
        List<Card> candidates = new List<Card>();
        if (deckCards == null)
        {
            return candidates;
        }

        foreach (Card card in deckCards)
        {
            if (card != null && GetCharacterCards(deckCards, card.character).Count > 1)
            {
                candidates.Add(card);
            }
        }

        return candidates;
    }

    public static int GetOverflowCount(IReadOnlyList<Card> deckCards, Character character)
    {
        int limit = Mathf.Max(0, GameRules.Deck.maxCardsPerCharacter);
        return Mathf.Max(0, GetCharacterCards(deckCards, character).Count - limit);
    }

    private static bool ContainsCardId(IReadOnlyList<Card> deckCards, int cardId)
    {
        if (deckCards == null)
        {
            return false;
        }

        foreach (Card card in deckCards)
        {
            if (card != null && card.cardId == cardId)
            {
                return true;
            }
        }

        return false;
    }

    private static void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temp;
        }
    }
}
