using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 카드 데이터를 관리하는 Static 클래스
/// CardCollection SO를 로드하여 Dictionary로 캐싱합니다.
/// </summary>
public static class CardManager
{
    [System.Serializable]
    private class CardRuntimeJsonRoot
    {
        public List<CardRuntimeJsonData> cards = null;
    }

    [System.Serializable]
    private class CardRuntimeJsonData
    {
        public int cardId = 0;
        public string description = string.Empty;
        public List<int> keywords = null;
    }

    [System.Serializable]
    private class CardGroupJsonRoot
    {
        public List<CardGroupJsonData> groups = null;
    }

    [System.Serializable]
    private class CardGroupJsonData
    {
        public string groupName = string.Empty;
        public List<int> cardIds = null;
    }

    private static Dictionary<int, CardData> cardCache;
    private static Dictionary<Character, List<CardData>> characterCache;
    private static Dictionary<int, List<int>> keywordCache;
    private static Dictionary<int, string> descriptionCache;
    private static Dictionary<string, List<int>> groupCache;
    private static bool isInitialized = false;
    
    /// <summary>
    /// 게임 시작 시 자동으로 초기화
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        // CardCollection SO 로드
        CardCollection collection = Resources.Load<CardCollection>("CardCollection");
        
        if (collection == null)
        {
            Debug.LogError("[CardManager] CardCollection not found in Resources folder!");
            return;
        }

        ApplyRuntimeCardData(collection.allCards);
        
        // Dictionary 캐싱 (cardId로 조회)
        cardCache = new Dictionary<int, CardData>();
        foreach (var card in collection.allCards)
        {
            if (!cardCache.ContainsKey(card.cardId))
            {
                cardCache[card.cardId] = card;
            }
            else
            {
                Debug.LogWarning($"[CardManager] Duplicate cardId found: {card.cardId}");
            }
        }
        
        // 캐릭터별 캐싱 (빠른 필터링용)
        characterCache = collection.allCards
            .GroupBy(c => c.character)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        isInitialized = true;
        
        Debug.Log($"[CardManager] 초기화 완료: 총 {cardCache.Count}장의 카드 로드");
        foreach (var kvp in characterCache)
        {
            Debug.Log($"[CardManager] {kvp.Key}: {kvp.Value.Count}장");
        }
    }

    private static void ApplyRuntimeCardData(List<CardData> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        Dictionary<int, List<int>> runtimeKeywords = LoadRuntimeKeywords();
        Dictionary<int, string> runtimeDescriptions = LoadRuntimeDescriptions();

        foreach (CardData card in cards)
        {
            if (card == null)
            {
                continue;
            }

            if (runtimeKeywords.TryGetValue(card.cardId, out List<int> keywords))
            {
                card.keywords = keywords != null ? new List<int>(keywords) : new List<int>();
            }

            if (runtimeDescriptions.TryGetValue(card.cardId, out string description))
            {
                card.description = description ?? string.Empty;
            }
        }
    }

    private static Dictionary<int, List<int>> LoadRuntimeKeywords()
    {
        if (keywordCache != null)
        {
            return keywordCache;
        }

        keywordCache = new Dictionary<int, List<int>>();
        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>("JsonData");
        foreach (TextAsset jsonFile in jsonFiles)
        {
            if (jsonFile == null || string.IsNullOrWhiteSpace(jsonFile.name))
            {
                continue;
            }

            if (!jsonFile.name.EndsWith("Cards"))
            {
                continue;
            }

            CardRuntimeJsonRoot root;
            try
            {
                root = JsonUtility.FromJson<CardRuntimeJsonRoot>(jsonFile.text);
            }
            catch
            {
                continue;
            }

            if (root?.cards == null)
            {
                continue;
            }

            foreach (CardRuntimeJsonData jsonCard in root.cards)
            {
                if (jsonCard == null || jsonCard.cardId <= 0)
                {
                    continue;
                }

                keywordCache[jsonCard.cardId] = jsonCard.keywords != null
                    ? new List<int>(jsonCard.keywords)
                    : new List<int>();
            }
        }

        return keywordCache;
    }

    private static Dictionary<int, string> LoadRuntimeDescriptions()
    {
        if (descriptionCache != null)
        {
            return descriptionCache;
        }

        descriptionCache = new Dictionary<int, string>();
        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>("JsonData");
        foreach (TextAsset jsonFile in jsonFiles)
        {
            if (jsonFile == null || string.IsNullOrWhiteSpace(jsonFile.name))
            {
                continue;
            }

            if (!jsonFile.name.EndsWith("Cards"))
            {
                continue;
            }

            CardRuntimeJsonRoot root;
            try
            {
                root = JsonUtility.FromJson<CardRuntimeJsonRoot>(jsonFile.text);
            }
            catch
            {
                continue;
            }

            if (root?.cards == null)
            {
                continue;
            }

            foreach (CardRuntimeJsonData jsonCard in root.cards)
            {
                if (jsonCard == null || jsonCard.cardId <= 0)
                {
                    continue;
                }

                descriptionCache[jsonCard.cardId] = jsonCard.description ?? string.Empty;
            }
        }

        return descriptionCache;
    }
    
    /// <summary>
    /// 카드 ID로 CardData 조회 (O(1))
    /// </summary>
    public static CardData GetCard(int cardId)
    {
        if (!isInitialized) Initialize();
        
        if (cardCache.ContainsKey(cardId))
        {
            return cardCache[cardId];
        }
        
        Debug.LogWarning($"[CardManager] Card not found: {cardId}");
        return null;
    }
    
    /// <summary>
    /// 카드 ID로 Card 객체 조회 (CardData를 Card로 변환)
    /// </summary>
    public static Card GetCardAsCard(int cardId)
    {
        CardData cardData = GetCard(cardId);
        
        if (cardData != null)
        {
            return cardData.ToCard();
        }
        
        Debug.LogWarning($"[CardManager] Card not found: {cardId}");
        return null;
    }
    
    /// <summary>
    /// 캐릭터별 카드 목록 조회 (O(1))
    /// </summary>
    public static List<CardData> GetCardsByCharacter(Character character)
    {
        if (!isInitialized) Initialize();
        
        if (characterCache.ContainsKey(character))
        {
            return characterCache[character];
        }
        
        Debug.LogWarning($"[CardManager] No cards found for character: {character}");
        return new List<CardData>();
    }
    
    /// <summary>
    /// 모든 카드 조회
    /// </summary>
    public static List<CardData> GetAllCards()
    {
        if (!isInitialized) Initialize();
        
        return cardCache.Values.ToList();
    }

    public static List<int> GetCardIdsByGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return new List<int>();
        }

        EnsureGroupCacheLoaded();
        if (groupCache != null && groupCache.TryGetValue(groupName, out List<int> cardIds))
        {
            return new List<int>(cardIds);
        }

        Debug.LogWarning($"[CardManager] Card group not found: {groupName}");
        return new List<int>();
    }
    
    /// <summary>
    /// 초기화 여부 확인
    /// </summary>
    public static bool IsInitialized()
    {
        return isInitialized;
    }

    private static void EnsureGroupCacheLoaded()
    {
        if (groupCache != null)
        {
            return;
        }

        groupCache = new Dictionary<string, List<int>>();
        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/cardGroups");
        if (jsonFile == null)
        {
            Debug.LogWarning("[CardManager] cardGroups.json을 불러오지 못했습니다");
            return;
        }

        CardGroupJsonRoot root = JsonUtility.FromJson<CardGroupJsonRoot>(jsonFile.text);
        if (root?.groups == null)
        {
            Debug.LogWarning("[CardManager] cardGroups.json 형식이 올바르지 않습니다");
            return;
        }

        foreach (CardGroupJsonData groupData in root.groups)
        {
            if (groupData == null || string.IsNullOrWhiteSpace(groupData.groupName))
            {
                continue;
            }

            groupCache[groupData.groupName] = groupData.cardIds != null
                ? new List<int>(groupData.cardIds)
                : new List<int>();
        }
    }
}

