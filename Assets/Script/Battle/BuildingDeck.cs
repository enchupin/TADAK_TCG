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
                    if (newCard != null)
                    {
                        deckList.Add(newCard);
                    }
                }
            }
        }

        Debug.Log($"[BuildingDeck] initialized: total {deckList.Count} cards (characters: {characters.Count})");
    }

    /// <summary>
    /// Add reward card.
    /// </summary>
    public void AddCard(int cardId)
    {
        Card newCard = CardManager.GetCardAsCard(cardId);
        if (newCard != null)
        {
            deckList.Add(newCard);
            Debug.Log($"[BuildingDeck] card added: {newCard.cardName} (total {deckList.Count})");
        }
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
            clonedCard.description = sourceCard.description;
            clonedCard.enforceCardIds = sourceCard.enforceCardIds != null
                ? new List<int>(sourceCard.enforceCardIds)
                : new List<int>();
            clonedCard.artworkAddress = sourceCard.artworkAddress;
            clonedCard.effectAddress = sourceCard.effectAddress;
            clonedCard.soundAddress = sourceCard.soundAddress;
            clonedCard.artwork = sourceCard.artwork;
            clonedCard.effectPrefab = sourceCard.effectPrefab;
            clonedCard.soundClip = sourceCard.soundClip;

            copiedDeck.Add(clonedCard);
        }

        return copiedDeck;
    }
}
