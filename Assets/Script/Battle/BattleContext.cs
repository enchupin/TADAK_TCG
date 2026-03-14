using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 전투 상태를 담는 컨텍스트 클래스
/// 효과 실행 시 필요한 모든 정보를 제공합니다.
/// </summary>
public class BattleContext
{
    // 카드 사용 통계
    private int cardsPlayedThisCombat;
    private int cardsPlayedThisTurn;
    private readonly List<Card> cardsPlayedThisTurnList = new List<Card>();
    private readonly List<Card> cardsPlayedThisCombatList = new List<Card>();

    // 카드 이동 관련
    public int cardsDiscardedThisTurn;
    public int cardsExhaustedThisTurn;
    public int cardsDrawnThisTurn;
    public int deckShuffleCountThisCombat;

    // 데미지 관련
    public int lastDamageDealt;
    public int totalDamageDealt;

    // 보호막 관련
    public int defenseConsumed;

    // 선택된 카드 (Choice -> Effect 연계용)
    private List<Card> selectedCards = new List<Card>();
    private readonly Dictionary<string, List<Card>> contextCardsBySubject = new Dictionary<string, List<Card>>(System.StringComparer.OrdinalIgnoreCase);

    public void SetSelectedCards(List<Card> cards)
    {
        selectedCards = new List<Card>(cards);
    }

    public List<Card> GetSelectedCards()
    {
        return new List<Card>(selectedCards);
    }

    private void ClearSelectedCards()
    {
        selectedCards.Clear();
    }

    public void SetContextCards(string subject, List<Card> cards)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return;
        }

        if (cards == null || cards.Count == 0) {
            contextCardsBySubject.Remove(subject);
            return;
        }

        contextCardsBySubject[subject] = new List<Card>(cards);
    }

    public List<Card> GetContextCards(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return new List<Card>();
        }

        if (contextCardsBySubject.TryGetValue(subject, out List<Card> cards) && cards != null) {
            return new List<Card>(cards);
        }

        return new List<Card>();
    }

    public Card GetContextCard(string subject)
    {
        List<Card> cards = GetContextCards(subject);
        return cards.Count > 0 ? cards[0] : null;
    }

    public void ClearContextCards(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) {
            return;
        }

        contextCardsBySubject.Remove(subject);
    }

    private void ClearAllContextCards()
    {
        contextCardsBySubject.Clear();
    }

    /// <summary>
    /// 턴 시작 시 호출
    /// </summary>
    public void OnTurnStart()
    {
        cardsPlayedThisTurn = 0;
        cardsPlayedThisTurnList.Clear();
        cardsDiscardedThisTurn = 0;
        cardsExhaustedThisTurn = 0;
        cardsDrawnThisTurn = 0;
        totalDamageDealt = 0;
        defenseConsumed = 0;
        ClearSelectedCards();
        ClearAllContextCards();
    }
    
    /// <summary>
    /// 전투 시작 시 호출
    /// </summary>
    public void OnCombatStart()
    {
        cardsPlayedThisCombat = 0;
        cardsPlayedThisCombatList.Clear();
        deckShuffleCountThisCombat = 0;
        totalDamageDealt = 0;
        OnTurnStart();
    }

    public void OnCardPlayed(Card card)
    {
        cardsPlayedThisTurn++;
        cardsPlayedThisCombat++;

        if (card != null)
        {
            cardsPlayedThisTurnList.Add(card);
            cardsPlayedThisCombatList.Add(card);
            Debug.Log($"[BattleContext] Card played. id={card.cardId}, turnCount={cardsPlayedThisTurn}, combatCount={cardsPlayedThisCombat}");
        }
    }

    public int GetCardsPlayedThisCombatCount(List<int> targetCardIds)
    {
        if (targetCardIds == null || targetCardIds.Count == 0)
            return cardsPlayedThisCombat;

        int count = 0;
        foreach (Card playedCard in cardsPlayedThisCombatList)
        {
            if (playedCard != null && targetCardIds.Contains(playedCard.cardId))
            {
                count++;
            }
        }

        return count;
    }

    public int GetCardsPlayedThisTurnCount(List<int> targetCardIds)
    {
        if (targetCardIds == null || targetCardIds.Count == 0)
            return cardsPlayedThisTurn;

        int count = 0;
        foreach (Card playedCard in cardsPlayedThisTurnList)
        {
            if (playedCard != null && targetCardIds.Contains(playedCard.cardId))
            {
                count++;
            }
        }

        return count;
    }

    public Card GetLastPlayedCard()
    {
        if (cardsPlayedThisTurnList == null || cardsPlayedThisTurnList.Count == 0) {
            return null;
        }

        return cardsPlayedThisTurnList[cardsPlayedThisTurnList.Count - 1];
    }

    public void OnCardsDrawn(int count)
    {
        if (count <= 0)
            return;

        cardsDrawnThisTurn += count;
    }

    public void OnCardsDiscarded(int count)
    {
        if (count <= 0)
            return;

        cardsDiscardedThisTurn += count;
    }

    public void OnCardsExhausted(int count)
    {
        if (count <= 0)
            return;

        cardsExhaustedThisTurn += count;
    }

    public void OnDeckShuffled()
    {
        deckShuffleCountThisCombat++;
    }

    public void OnDamageDealt(int amount)
    {
        if (amount <= 0)
            return;

        lastDamageDealt = amount;
        totalDamageDealt += amount;
    }

    public void OnDefenseConsumed(int amount)
    {
        defenseConsumed += amount;
    }
}
