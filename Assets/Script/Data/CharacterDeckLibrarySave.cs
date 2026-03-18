using System;
using System.Collections.Generic;

[Serializable]
public class CharacterDeckLibrarySave
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

        for (int i = 0; i < decks.Count; i++)
        {
            CharacterDeckSave deck = decks[i];
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
