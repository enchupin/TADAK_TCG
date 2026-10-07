// ReSharper disable CheckNamespace
using System;
using System.Collections.Generic;


// 캐릭터 직업별 덱 리스트 클래스
[Serializable]
public class CharacterDeckListSave
{
    public int characterId;
    public string selectedDeckId = string.Empty;
    public List<CharacterDeckSave> decks = new();

    public CharacterDeckSave FindDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId) || decks == null)
        {
            return null;
        }

        foreach (CharacterDeckSave deck in decks)
        {
            if (deck == null)
            {
                continue;
            }

            if (string.Equals(deck.deckId, deckId, StringComparison.Ordinal))
            {
                return deck;
            }
        }

        return null;
    }

    public CharacterDeckSave GetSelectedDeck()
    {
        if (decks == null || decks.Count == 0)
        {
            return null;
        }

        CharacterDeckSave selectedDeck = FindDeck(selectedDeckId);
        if (selectedDeck != null)
        {
            return selectedDeck;
        }

        return decks[0];
    }
}
