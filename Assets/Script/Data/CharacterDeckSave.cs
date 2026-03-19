// ReSharper disable CheckNamespace
using System;
using System.Collections.Generic;

[Serializable]
public class CharacterDeckSave
{
    public string deckId = string.Empty;
    public string name = string.Empty;
    public string createdAtUtc = string.Empty;
    public List<int> cardIds = new();

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
