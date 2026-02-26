using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 상태를 담는 컨텍스트 클래스
/// 효과 실행 시 필요한 모든 정보를 제공합니다.
/// </summary>
public class BattleContext {

    // 카드 사용 통계
    public int cardsPlayedThisCombat = 0;
    public int cardsPlayedThisTurn = 0;
    public List<Card> cardsPlayedThisTurnList = new List<Card>(); // 추후 연동 예정

    // 카드 이동 관련
    public int cardsDiscardedThisTurn = 0;
    public int cardsExhaustedThisTurn = 0;
    public int cardsDrawnThisTurn = 0;

    // 데미지 관련
    public int lastDamageDealt = 0; // 추후 연동 예정
    public int totalDamageDealt = 0; // 추후 연동 예정

    // 방어도 관련
    public int defenseConsumed = 0; // 추후 연동 예정


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
    
    public void ClearSelectedCards()
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
        totalDamageDealt = 0;
        OnTurnStart();
    }
    
    /// <summary>
    /// 카드 사용 시 호출
    /// </summary>
    public void OnCardPlayed(Card card)
    {
        cardsPlayedThisTurn++;
        cardsPlayedThisCombat++;
        cardsPlayedThisTurnList.Add(card);
    }

    /// <summary>
    /// 카드 드로우 시 호출
    /// </summary>
    public void OnCardsDrawn(int count)
    {
        if (count <= 0)
            return;

        cardsDrawnThisTurn += count;
    }

    /// <summary>
    /// 카드 버림 시 호출
    /// </summary>
    public void OnCardsDiscarded(int count)
    {
        if (count <= 0)
            return;

        cardsDiscardedThisTurn += count;
    }

    /// <summary>
    /// 카드 소멸 시 호출
    /// </summary>
    public void OnCardsExhausted(int count)
    {
        if (count <= 0)
            return;

        cardsExhaustedThisTurn += count;
    }
    
    /// <summary>
    /// 데미지 입힌 후 호출
    /// </summary>
    public void OnDamageDealt(int amount)
    {
        if (amount <= 0)
            return;

        lastDamageDealt = amount;
        totalDamageDealt += amount;
    }
    
    /// <summary>
    /// 방어도 소모 시 호출
    /// </summary>
    public void OnDefenseConsumed(int amount)
    {
        defenseConsumed += amount;
    }
}
