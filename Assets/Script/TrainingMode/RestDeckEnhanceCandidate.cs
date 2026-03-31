using System.Collections.Generic;

public sealed class RestDeckEnhanceCandidate
{
    public Character character;
    public int deckIndex;
    public int sourceCardId;
    public string cardName = string.Empty;
    public string characterName = string.Empty;
    public int duplicateOrder;
    public List<int> enhanceCardIds = new List<int>();
}
