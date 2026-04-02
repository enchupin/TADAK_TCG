using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent deck for a training run.
/// </summary>
public class BuildingDeck
{
    // Cards permanently owned during the current run.
    private List<Card> deckList = new List<Card>();

    /// <summary>
    /// Initialize with selected characters' starter cards.
    /// </summary>
    public void Initialize(List<Character> characters)
    {
        deckList.Clear();
        foreach (var character in characters)
        {
            CharacterData data = CharacterManager.GetCharacterByEnum(character);
            if (data != null && data.startDeckCardIds != null)
            {
                foreach (int cardId in data.startDeckCardIds)
                {
                    Card newCard = CardManager.GetCardAsCard(cardId);
                    if (newCard == null)
                    {
                        continue;
                    }

                    if (ViolatesUniqueRule(newCard))
                    {
                        Debug.LogWarning($"[BuildingDeck] 유일 키워드로 인해 중복 카드를 건너뜁니다: {newCard.cardName}");
                        continue;
                    }

                    deckList.Add(newCard);
                }
            }
        }

        Debug.Log($"[BuildingDeck] initialized: total {deckList.Count} cards (characters: {characters.Count})");
    }

    public void InitializeFromCardIds(List<int> cardIds)
    {
        deckList.Clear();

        if (cardIds == null)
        {
            Debug.Log("[BuildingDeck] cardId 목록이 없어 빈 덱으로 초기화했습니다");
            return;
        }

        foreach (int cardId in cardIds)
        {
            Card newCard = CardManager.GetCardAsCard(cardId);
            if (newCard == null)
            {
                Debug.LogWarning($"[BuildingDeck] cardId={cardId} 카드 생성에 실패했습니다");
                continue;
            }

            if (ViolatesUniqueRule(newCard))
            {
                Debug.LogWarning($"[BuildingDeck] 유일 키워드로 인해 중복 카드를 건너뜁니다: {newCard.cardName}");
                continue;
            }

            deckList.Add(newCard);
        }

        Debug.Log($"[BuildingDeck] cardId 기준 초기화 완료: total {deckList.Count} cards");
    }

    /// <summary>
    /// Add reward card.
    /// </summary>
    public void AddCard(int cardId)
    {
        Card newCard = CardManager.GetCardAsCard(cardId);
        if (newCard == null)
        {
            return;
        }

        if (ViolatesUniqueRule(newCard))
        {
            Debug.LogWarning($"[BuildingDeck] 유일 키워드로 인해 카드를 추가할 수 없습니다: {newCard.cardName}");
            return;
        }

        deckList.Add(newCard);
        Debug.Log($"[BuildingDeck] card added: {newCard.cardName} (total {deckList.Count})");
    }

    public bool ReplaceCardAt(int index, int cardId)
    {
        if (index < 0 || index >= deckList.Count)
        {
            Debug.LogWarning($"[BuildingDeck] 교체할 카드 인덱스가 범위를 벗어났습니다: {index}");
            return false;
        }

        Card newCard = CardManager.GetCardAsCard(cardId);
        if (newCard == null)
        {
            Debug.LogWarning($"[BuildingDeck] 교체 카드 생성에 실패했습니다: {cardId}");
            return false;
        }

        if (ViolatesUniqueRule(newCard, index))
        {
            Debug.LogWarning($"[BuildingDeck] 유일 키워드로 인해 카드를 교체할 수 없습니다: {newCard.cardName}");
            return false;
        }

        deckList[index] = newCard;
        Debug.Log($"[BuildingDeck] card replaced at {index}: {newCard.cardName}");
        return true;
    }

    /// <summary>
    /// Remove card from run deck.
    /// </summary>
    public void RemoveCard(Card card)
    {
        if (deckList.Contains(card))
        {
            deckList.Remove(card);
            Debug.Log($"[BuildingDeck] card removed: {card.cardName} (total {deckList.Count})");
        }
        else
        {
            Debug.LogWarning($"[BuildingDeck] card not found for removal: {card.cardName}");
        }
    }

    /// <summary>
    /// Returns a battle-safe deck copy (new Card instances, fresh effect instances).
    /// </summary>
    public List<Card> CopyDeck()
    {
        List<Card> copiedDeck = new List<Card>(deckList.Count);

        foreach (Card sourceCard in deckList)
        {
            if (sourceCard == null)
                continue;

            // Rebuild from CardData so each copy has independent effect instances.
            Card clonedCard = CardManager.GetCardAsCard(sourceCard.cardId);
            if (clonedCard == null)
            {
                Debug.LogWarning($"[BuildingDeck] clone failed for cardId={sourceCard.cardId}, fallback to original reference.");
                copiedDeck.Add(sourceCard);
                continue;
            }

            // Preserve metadata that may have been adjusted on the source card.
            clonedCard.cardName = sourceCard.cardName;
            clonedCard.character = sourceCard.character;
            clonedCard.cost = sourceCard.cost;
            clonedCard.baseCost = sourceCard.baseCost;
            clonedCard.description = sourceCard.description;
            clonedCard.enforceCardIds = sourceCard.enforceCardIds != null
                ? new List<int>(sourceCard.enforceCardIds)
                : new List<int>();
            clonedCard.keywords = sourceCard.keywords != null
                ? new List<int>(sourceCard.keywords)
                : new List<int>();

            copiedDeck.Add(clonedCard);
        }

        return copiedDeck;
    }

    public List<int> GetCardIds()
    {
        List<int> cardIds = new List<int>(deckList.Count);

        foreach (Card card in deckList)
        {
            if (card == null)
            {
                continue;
            }

            cardIds.Add(card.cardId);
        }

        return cardIds;
    }

    private bool ViolatesUniqueRule(Card newCard, int ignoredIndex = -1)
    {
        if (newCard == null)
        {
            return false;
        }

        for (int i = 0; i < deckList.Count; i++)
        {
            if (i == ignoredIndex)
            {
                continue;
            }

            Card existingCard = deckList[i];
            if (existingCard == null)
            {
                continue;
            }

            if (!string.Equals(existingCard.cardName, newCard.cardName))
            {
                continue;
            }

            if (existingCard.HasKeyword(CardKeywordIds.Unique) || newCard.HasKeyword(CardKeywordIds.Unique))
            {
                return true;
            }
        }

        return false;
    }
}

public static class TrainingRunDeckPersistence
{
    private const string DefaultDeckName = "기본 덱";
    private const string BossClearSaveReasonLog = "보스 클리어로 저장덱을 갱신했습니다";
    private const string EventEscapeSaveReasonLog = "이벤트 탈출로 저장덱을 갱신했습니다";

    public static BuildingDeck CreateRunDeck(List<Character> selectedCharacters)
    {
        BuildingDeck deck = new BuildingDeck();
        deck.InitializeFromCardIds(LoadPermanentDeckCardIds(selectedCharacters));
        return deck;
    }

    public static void SaveRunDeckOnBossClear(BuildingDeck runDeck, List<Character> selectedCharacters)
    {
        SaveRunDeckAsPermanentDeck(runDeck, selectedCharacters, BossClearSaveReasonLog);
    }

    public static void SaveRunDeckOnEventEscape(BuildingDeck runDeck, List<Character> selectedCharacters)
    {
        SaveRunDeckAsPermanentDeck(runDeck, selectedCharacters, EventEscapeSaveReasonLog);
    }

    public static void SaveRunDeckAsPermanentDeck(
        BuildingDeck runDeck,
        List<Character> selectedCharacters,
        string saveReasonLog = "저장덱을 갱신했습니다")
    {
        if (runDeck == null || selectedCharacters == null || selectedCharacters.Count == 0)
        {
            return;
        }

        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        if (profile == null)
        {
            Debug.LogWarning("[TrainingRunDeckPersistence] 프로필이 없어 영구덱을 저장할 수 없습니다");
            return;
        }

        List<Card> runCards = runDeck.CopyDeck();
        HashSet<Character> handledCharacters = new HashSet<Character>();
        bool hasChanges = false;

        foreach (Character character in selectedCharacters)
        {
            if (!handledCharacters.Add(character))
            {
                continue;
            }

            CharacterDeckLibrarySave library = ProfileSaveManager.GetOrCreateLibrary(character);
            if (library == null)
            {
                continue;
            }

            CharacterDeckSave deckSave = EnsureSelectedDeck(library, character);
            if (deckSave == null)
            {
                continue;
            }

            deckSave.cardIds = ExtractCardIdsForCharacter(runCards, character);
            hasChanges = true;
        }

        if (!hasChanges)
        {
            return;
        }

        ProfileSaveManager.Save(profile);
        Debug.Log($"[TrainingRunDeckPersistence] {saveReasonLog}");
    }

    private static List<int> LoadPermanentDeckCardIds(List<Character> selectedCharacters)
    {
        List<int> mergedCardIds = new List<int>();
        if (selectedCharacters == null || selectedCharacters.Count == 0)
        {
            return mergedCardIds;
        }

        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        HashSet<Character> handledCharacters = new HashSet<Character>();
        bool shouldSaveProfile = false;

        foreach (Character character in selectedCharacters)
        {
            if (!handledCharacters.Add(character))
            {
                continue;
            }

            CharacterDeckLibrarySave library = ProfileSaveManager.GetOrCreateLibrary(character);
            if (library == null)
            {
                mergedCardIds.AddRange(CharacterManager.GetStartDeck(character));
                continue;
            }

            CharacterDeckSave selectedDeck = library.GetSelectedDeck();
            if (selectedDeck == null)
            {
                selectedDeck = CreateStarterDeck(library, character);
                shouldSaveProfile = true;
            }

            if (selectedDeck?.cardIds != null)
            {
                mergedCardIds.AddRange(selectedDeck.cardIds);
            }
        }

        if (shouldSaveProfile && profile != null)
        {
            ProfileSaveManager.Save(profile);
        }

        return mergedCardIds;
    }

    private static CharacterDeckSave EnsureSelectedDeck(CharacterDeckLibrarySave library, Character character)
    {
        CharacterDeckSave selectedDeck = library?.GetSelectedDeck();
        if (selectedDeck != null)
        {
            return selectedDeck;
        }

        return CreateStarterDeck(library, character);
    }

    private static CharacterDeckSave CreateStarterDeck(CharacterDeckLibrarySave library, Character character)
    {
        if (library == null)
        {
            return null;
        }

        CharacterDeckSave starterDeck = CharacterDeckSave.Create(DefaultDeckName, CharacterManager.GetStartDeck(character));
        library.decks.Add(starterDeck);
        library.selectedDeckId = starterDeck.deckId;
        return starterDeck;
    }

    private static List<int> ExtractCardIdsForCharacter(List<Card> cards, Character character)
    {
        List<int> cardIds = new List<int>();
        if (cards == null)
        {
            return cardIds;
        }

        foreach (Card card in cards)
        {
            if (card == null || card.character != character)
            {
                continue;
            }

            cardIds.Add(card.cardId);
        }

        return cardIds;
    }
}
