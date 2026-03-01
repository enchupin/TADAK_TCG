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

    // 데미지 관련
    public int lastDamageDealt;
    public int totalDamageDealt;

    // 방어도 관련
    public int defenseConsumed;

    // 선택된 카드 (Choice -> Effect 연계용)
    private List<Card> selectedCards = new List<Card>();
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
    }
    
    /// <summary>
    /// 전투 시작 시 호출
    /// </summary>
    public void OnCombatStart()
    {
        cardsPlayedThisCombat = 0;
        cardsPlayedThisCombatList.Clear();
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
