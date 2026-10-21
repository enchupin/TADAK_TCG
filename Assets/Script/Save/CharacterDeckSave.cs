// ReSharper disable CheckNamespace
using System;
using System.Collections.Generic;

[Serializable]
public class CharacterDeckSave
{
    public const int MaxCardCount = 8;

    public static bool IsValidCardCount(int count)
    {
        return count > 0 && count <= MaxCardCount;
    }

    public string deckId = string.Empty;
    public string name = string.Empty;
    public string createdAtUtc = string.Empty;
    public bool isRunSavedDeck = false;
    public string runSavedAtUtc = string.Empty;
    public List<int> cardIds = new();
    // R-22a cardIds와 같은 위치의 영구 피해 증가량 (비어 있으면 0, 보스전 시작 시 초기화)
    public List<int> permanentDamageBonuses = new();

    public static CharacterDeckSave Create(string deckName, List<int> sourceCardIds = null)
    {
        CharacterDeckSave deck = new CharacterDeckSave
        {
            deckId = Guid.NewGuid().ToString("N"),
            name = string.IsNullOrWhiteSpace(deckName) ? "새 덱" : deckName,
            createdAtUtc = DateTime.UtcNow.ToString("o"),
            cardIds = sourceCardIds != null ? new List<int>(sourceCardIds) : new List<int>()
        };

        return deck;
    }
}
