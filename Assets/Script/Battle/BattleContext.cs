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
    public List<Card> cardsPlayedThisTurnList = new List<Card>();
    
    // 데미지 관련
    public int lastDamageDealt = 0;
    public int totalDamageDealt = 0;
    
    // 방어도 관련
    public int defenseConsumed = 0;
    
    // 카드 이동 관련
    public int cardsDiscardedThisTurn = 0;
    public int cardsExhaustedThisTurn = 0;
    public int cardsDrawnThisTurn = 0;
    
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
        defenseConsumed = 0;
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
    /// 데미지 입힌 후 호출
    /// </summary>
    public void OnDamageDealt(int amount)
    {
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
