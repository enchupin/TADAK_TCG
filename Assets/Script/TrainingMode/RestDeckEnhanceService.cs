using System.Collections.Generic;
using UnityEngine;

public static class RestDeckEnhanceService
{
    public static List<RestDeckEnhanceCandidate> GetEnhanceableCandidates(BuildingDeck useDeck)
    {
        List<RestDeckEnhanceCandidate> candidates = new List<RestDeckEnhanceCandidate>();
        if (useDeck == null)
        {
            return candidates;
        }

        List<int> cardIds = useDeck.GetCardIds();
        Dictionary<int, int> duplicateCounts = new Dictionary<int, int>();
        for (int i = 0; i < cardIds.Count; i++)
        {
            CardData sourceCardData = CardManager.GetCard(cardIds[i]);
            if (sourceCardData == null || sourceCardData.enforceCardIds == null || sourceCardData.enforceCardIds.Count == 0)
            {
                continue;
            }

            List<int> validEnhanceIds = GetValidEnhanceIds(cardIds, i, sourceCardData.enforceCardIds);
            if (validEnhanceIds.Count <= 0)
            {
                continue;
            }

            duplicateCounts.TryGetValue(cardIds[i], out int order);
            duplicateCounts[cardIds[i]] = ++order;
            CharacterData characterData = CharacterManager.GetCharacterByEnum(sourceCardData.character);

            candidates.Add(new RestDeckEnhanceCandidate
            {
                character = sourceCardData.character,
                deckIndex = i,
                sourceCardId = cardIds[i],
                cardName = string.IsNullOrWhiteSpace(sourceCardData.cardName) ? cardIds[i].ToString() : sourceCardData.cardName,
                characterName = characterData != null ? characterData.characterName : sourceCardData.character.ToString(),
                duplicateOrder = order,
                enhanceCardIds = validEnhanceIds
            });
        }

        return candidates;
    }

    public static List<int> GetRandomEnhanceOptions(RestDeckEnhanceCandidate candidate, int optionCount = 3)
    {
        List<int> options = candidate?.enhanceCardIds != null
            ? new List<int>(candidate.enhanceCardIds)
            : new List<int>();

        for (int i = 0; i < options.Count; i++)
        {
            int swapIndex = Random.Range(i, options.Count);
            int temp = options[i];
            options[i] = options[swapIndex];
            options[swapIndex] = temp;
        }

        if (options.Count > optionCount)
        {
            options.RemoveRange(optionCount, options.Count - optionCount);
        }

        return options;
    }

    public static bool TryApplyEnhance(BuildingDeck useDeck, RestDeckEnhanceCandidate candidate, int enhanceCardId)
    {
        if (useDeck == null || candidate == null || enhanceCardId <= 0)
        {
            return false;
        }

        List<int> cardIds = useDeck.GetCardIds();
        if (candidate.deckIndex < 0 || candidate.deckIndex >= cardIds.Count || cardIds[candidate.deckIndex] != candidate.sourceCardId)
        {
            Debug.LogWarning("[RestDeckEnhanceService] 강화 대상 카드 위치가 변경되었습니다");
            return false;
        }

        CardData sourceCardData = CardManager.GetCard(candidate.sourceCardId);
        List<int> validEnhanceIds = sourceCardData != null
            ? GetValidEnhanceIds(cardIds, candidate.deckIndex, sourceCardData.enforceCardIds)
            : new List<int>();
        if (!validEnhanceIds.Contains(enhanceCardId))
        {
            Debug.LogWarning("[RestDeckEnhanceService] 유효하지 않은 강화 루트를 선택했습니다");
            return false;
        }

        return useDeck.ReplaceCardAt(candidate.deckIndex, enhanceCardId);
    }

    private static List<int> GetValidEnhanceIds(List<int> deckCardIds, int replaceIndex, List<int> candidateEnhanceIds)
    {
        List<int> result = new List<int>();
        if (deckCardIds == null || candidateEnhanceIds == null || replaceIndex < 0 || replaceIndex >= deckCardIds.Count)
        {
            return result;
        }

        HashSet<int> seen = new HashSet<int>();
        foreach (int enhanceCardId in candidateEnhanceIds)
        {
            if (enhanceCardId <= 0 || !seen.Add(enhanceCardId) || !CanReplaceCard(deckCardIds, replaceIndex, enhanceCardId))
            {
                continue;
            }

            result.Add(enhanceCardId);
        }

        return result;
    }

    private static bool CanReplaceCard(List<int> deckCardIds, int replaceIndex, int enhanceCardId)
    {
        Card enhanceCard = CardManager.GetCardAsCard(enhanceCardId);
        if (enhanceCard == null)
        {
            return false;
        }

        for (int i = 0; i < deckCardIds.Count; i++)
        {
            if (i == replaceIndex)
            {
                continue;
            }

            Card existingCard = CardManager.GetCardAsCard(deckCardIds[i]);
            if (existingCard == null)
            {
                continue;
            }

            if (!string.Equals(existingCard.cardName, enhanceCard.cardName, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (existingCard.HasKeyword(CardKeywordIds.Unique) || enhanceCard.HasKeyword(CardKeywordIds.Unique))
            {
                return false;
            }
        }

        return true;
    }
}
