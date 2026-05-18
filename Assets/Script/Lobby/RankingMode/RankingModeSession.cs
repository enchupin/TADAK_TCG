using System.Collections.Generic;
using UnityEngine;

public sealed class RankingModeSelectedDeck
{
    public Character character;
    public string deckId;
    public string deckName;
    public List<int> cardIds = new List<int>();

    public RankingModeSelectedDeck(Character character, CharacterDeckSave deck)
    {
        this.character = character;
        deckId = deck != null ? deck.deckId : string.Empty;
        deckName = deck != null ? deck.name : string.Empty;
        cardIds = deck?.cardIds != null ? new List<int>(deck.cardIds) : new List<int>();
    }
}

public static class RankingModeSession
{
    public const int RequiredSelectionCount = 3;
    public const int TurnLimit = 10;
    public const string BestDamagePlayerPrefsKey = "RankingModeBestDamage";

    private static readonly List<RankingModeSelectedDeck> selectedDecks = new List<RankingModeSelectedDeck>();

    public static bool IsActive { get; private set; }
    public static int TotalDamage { get; private set; }

    public static bool TryStartFromSelectedDecks(IReadOnlyList<RankingModeSelectedDeck> sourceDecks)
    {
        if (!HasValidDeckSelection(sourceDecks))
        {
            Debug.LogWarning("[RankingModeSession] 랭킹모드는 서로 다른 캐릭터의 저장덱 3개를 선택해야 시작할 수 있습니다");
            return false;
        }

        selectedDecks.Clear();
        foreach (RankingModeSelectedDeck sourceDeck in sourceDecks)
        {
            if (sourceDeck == null)
            {
                continue;
            }

            selectedDecks.Add(new RankingModeSelectedDeck(sourceDeck.character, null)
            {
                deckId = sourceDeck.deckId,
                deckName = sourceDeck.deckName,
                cardIds = sourceDeck.cardIds != null ? new List<int>(sourceDeck.cardIds) : new List<int>()
            });
        }

        TotalDamage = 0;
        IsActive = true;
        Debug.Log("[RankingModeSession] 랭킹모드 세션을 시작합니다");
        return true;
    }

    public static List<Character> CopySelectedCharacters()
    {
        List<Character> characters = new List<Character>(selectedDecks.Count);
        foreach (RankingModeSelectedDeck selectedDeck in selectedDecks)
        {
            if (selectedDeck != null)
            {
                characters.Add(selectedDeck.character);
            }
        }

        return characters;
    }

    public static bool HasValidSelectedCharacters()
    {
        return HasValidDeckSelection(selectedDecks);
    }

    public static BuildingDeck CreateBattleDeck()
    {
        List<int> mergedCardIds = new List<int>();
        foreach (RankingModeSelectedDeck selectedDeck in selectedDecks)
        {
            if (selectedDeck?.cardIds != null)
            {
                mergedCardIds.AddRange(selectedDeck.cardIds);
            }
        }

        BuildingDeck deck = new BuildingDeck();
        deck.InitializeFromCardIds(mergedCardIds);
        return deck;
    }

    public static void AddDamage(int damage)
    {
        if (!IsActive || damage <= 0)
        {
            return;
        }

        TotalDamage += damage;
    }

    public static RankingModeResult CompleteAndSaveBestDamage()
    {
        int previousBestDamage = PlayerPrefs.GetInt(BestDamagePlayerPrefsKey, 0);
        int bestDamage = Mathf.Max(previousBestDamage, TotalDamage);
        bool isNewBest = bestDamage > previousBestDamage;

        if (isNewBest)
        {
            PlayerPrefs.SetInt(BestDamagePlayerPrefsKey, bestDamage);
            PlayerPrefs.Save();
        }

        RankingModeResult result = new RankingModeResult(TotalDamage, bestDamage, isNewBest);
        ResetSession();
        return result;
    }

    public static void ResetSession()
    {
        IsActive = false;
        TotalDamage = 0;
        selectedDecks.Clear();
    }

    private static bool HasValidDeckSelection(IReadOnlyList<RankingModeSelectedDeck> decks)
    {
        if (decks == null || decks.Count != RequiredSelectionCount)
        {
            return false;
        }

        HashSet<Character> selectedCharacters = new HashSet<Character>();
        foreach (RankingModeSelectedDeck deck in decks)
        {
            if (deck == null || deck.cardIds == null || deck.cardIds.Count == 0)
            {
                return false;
            }

            if (!selectedCharacters.Add(deck.character))
            {
                return false;
            }
        }

        return selectedCharacters.Count == RequiredSelectionCount;
    }
}

public readonly struct RankingModeResult
{
    public readonly int totalDamage;
    public readonly int bestDamage;
    public readonly bool isNewBest;

    public RankingModeResult(int totalDamage, int bestDamage, bool isNewBest)
    {
        this.totalDamage = totalDamage;
        this.bestDamage = bestDamage;
        this.isNewBest = isNewBest;
    }
}
