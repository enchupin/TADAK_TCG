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
            List<int> starterCardIds = CharacterManager.GetStarterCardIds(character);
            foreach (int cardId in starterCardIds)
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

        Debug.Log($"[BuildingDeck] initialized: total {deckList.Count} cards (characters: {characters.Count})");
    }

    public void InitializeFromCardIds(List<int> cardIds)
    {
        InitializeFromCardIds(cardIds, null);
    }

    // permanentDamageBonuses: cardIds와 같은 위치의 R-22a 영구 피해 증가량
    public void InitializeFromCardIds(List<int> cardIds, List<int> permanentDamageBonuses)
    {
        deckList.Clear();

        if (cardIds == null)
        {
            Debug.Log("[BuildingDeck] cardId 목록이 없어 빈 덱으로 초기화했습니다");
            return;
        }

        for (int index = 0; index < cardIds.Count; index++)
        {
            int cardId = cardIds[index];
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

            if (permanentDamageBonuses != null && index < permanentDamageBonuses.Count)
            {
                newCard.permanentDamageBonus = Mathf.Max(0, permanentDamageBonuses[index]);
            }

            deckList.Add(newCard);
        }

        Debug.Log($"[BuildingDeck] cardId 기준 초기화 완료: total {deckList.Count} cards");
    }

    /// <summary>
    /// Add reward card.
    /// </summary>
    public bool AddCard(int cardId)
    {
        Card newCard = CardManager.GetCardAsCard(cardId);
        if (newCard == null)
        {
            return false;
        }

        if (ViolatesUniqueRule(newCard))
        {
            Debug.LogWarning($"[BuildingDeck] 유일 키워드로 인해 카드를 추가할 수 없습니다: {newCard.cardName}");
            return false;
        }

        deckList.Add(newCard);
        Debug.Log($"[BuildingDeck] card added: {newCard.cardName} (total {deckList.Count})");
        return true;
    }

    // 저장 덱의 카드 인스턴스 목록 (선택 UI에서 제거 대상 지정용, 전투에는 CopyDeck 사용)
    public IReadOnlyList<Card> GetCards()
    {
        return deckList;
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

        for (int deckIndex = 0; deckIndex < deckList.Count; deckIndex++)
        {
            Card sourceCard = deckList[deckIndex];
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
            clonedCard.permanentDamageBonus = sourceCard.permanentDamageBonus;

            LocalizationManager.ApplyToRuntimeCard(clonedCard);
            // 호기심 강화를 저장 덱에 반영하기 위한 원본 위치
            clonedCard.sourceDeckIndex = deckIndex;

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

    // R-22a 덱 위치의 카드에 영구 피해 증가 누적
    public bool AddPermanentDamageBonusAt(int index, int cardId, int amount)
    {
        if (index < 0 || index >= deckList.Count || deckList[index] == null || deckList[index].cardId != cardId)
        {
            return false;
        }

        deckList[index].permanentDamageBonus = Mathf.Max(0, deckList[index].permanentDamageBonus + amount);
        return true;
    }

    public void RefreshLocalizedTexts()
    {
        foreach (Card card in deckList)
        {
            LocalizationManager.ApplyToRuntimeCard(card);
        }
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

    public static BuildingDeck CreateRunDeck(List<Character> selectedCharacters)
    {
        BuildingDeck deck = new BuildingDeck();
        List<int> permanentDamageBonuses = new List<int>();
        deck.InitializeFromCardIds(LoadPermanentDeckCardIds(selectedCharacters, permanentDamageBonuses), permanentDamageBonuses);
        return deck;
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

            CharacterDeckListSave library = ProfileSaveManager.GetOrCreateLibrary(character);
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
            deckSave.permanentDamageBonuses = ExtractPermanentDamageBonusesForCharacter(runCards, character);
            deckSave.isRunSavedDeck = true;
            deckSave.runSavedAtUtc = System.DateTime.UtcNow.ToString("o");
            hasChanges = true;
        }

        if (!hasChanges)
        {
            return;
        }

        ProfileSaveManager.Save(profile);
        Debug.Log($"[TrainingRunDeckPersistence] {saveReasonLog}");
    }

    private static List<int> LoadPermanentDeckCardIds(List<Character> selectedCharacters, List<int> permanentDamageBonuses)
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

            CharacterDeckListSave library = ProfileSaveManager.GetOrCreateLibrary(character);
            if (library == null)
            {
                List<int> starterCardIds = CharacterManager.GetStarterCardIds(character);
                mergedCardIds.AddRange(starterCardIds);
                AppendPermanentDamageBonuses(permanentDamageBonuses, null, starterCardIds.Count);
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
                AppendPermanentDamageBonuses(permanentDamageBonuses, selectedDeck.permanentDamageBonuses, selectedDeck.cardIds.Count);
            }
        }

        if (shouldSaveProfile && profile != null)
        {
            ProfileSaveManager.Save(profile);
        }

        return mergedCardIds;
    }

    private static CharacterDeckSave EnsureSelectedDeck(CharacterDeckListSave library, Character character)
    {
        CharacterDeckSave selectedDeck = library?.GetSelectedDeck();
        if (selectedDeck != null)
        {
            return selectedDeck;
        }

        return CreateStarterDeck(library, character);
    }

    private static CharacterDeckSave CreateStarterDeck(CharacterDeckListSave library, Character character)
    {
        if (library == null)
        {
            return null;
        }

        CharacterDeckSave starterDeck = CharacterDeckSave.Create(DefaultDeckName, CharacterManager.GetStarterCardIds(character));
        library.decks.Add(starterDeck);
        library.selectedDeckId = starterDeck.deckId;
        return starterDeck;
    }

    // 덱 카드 수만큼 위치를 맞춰 영구 피해 증가량 추가 (없는 위치는 0)
    private static void AppendPermanentDamageBonuses(List<int> target, List<int> source, int count)
    {
        if (target == null)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            target.Add(source != null && i < source.Count ? Mathf.Max(0, source[i]) : 0);
        }
    }

    // R-22a 보스전 시작 시 모든 저장 덱의 영구 피해 증가 초기화
    public static void ResetPermanentDamageBonuses()
    {
        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        if (profile?.characters == null)
        {
            return;
        }

        bool hasChanges = false;
        foreach (CharacterDeckListSave library in profile.characters)
        {
            if (library?.decks == null)
            {
                continue;
            }

            foreach (CharacterDeckSave deck in library.decks)
            {
                if (deck?.permanentDamageBonuses != null && deck.permanentDamageBonuses.Count > 0)
                {
                    deck.permanentDamageBonuses.Clear();
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            ProfileSaveManager.Save(profile);
        }
    }

    private static List<int> ExtractPermanentDamageBonusesForCharacter(List<Card> cards, Character character)
    {
        List<int> bonuses = new List<int>();
        bool hasBonus = false;
        if (cards == null)
        {
            return bonuses;
        }

        foreach (Card card in cards)
        {
            if (card == null || card.character != character)
            {
                continue;
            }

            bonuses.Add(Mathf.Max(0, card.permanentDamageBonus));
            hasBonus |= card.permanentDamageBonus > 0;
        }

        // 누적이 없으면 빈 목록으로 저장
        return hasBonus ? bonuses : new List<int>();
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
