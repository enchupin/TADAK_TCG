using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 상태를 담는 컨텍스트 클래스
/// 효과 실행 시 필요한 모든 정보를 제공합니다.
/// </summary>
public class BattleContext {
    // 플레이어 정보 (나중에 Player 클래스로 교체)
    public int playerHP;
    public int playerMaxHP;
    public int playerDefense;
    public int playerEnergy;
    public int playerStrength; // 힘 버프

    // 적 정보 (나중에 Monster 클래스로 교체)
    public int enemyHP;
    public int enemyMaxHP;
    public int enemyDefense;

    // 턴 정보
    public int cardsPlayedThisTurn;
    public List<Card> cardsPlayedThisTurnList = new List<Card>();

    // 카드 관리 (나중에 CardManager로 교체)
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discardPile = new List<Card>();

    // 효과 실행 헬퍼 메서드들

    /// <summary>
    /// 적에게 데미지를 입힙니다.
    /// </summary>
    public void DealDamage(int amount) {
        int finalDamage = amount + playerStrength; // 힘 버프 적용
        int damageAfterDefense = Mathf.Max(0, finalDamage - enemyDefense);

        enemyHP -= damageAfterDefense;
        enemyDefense = Mathf.Max(0, enemyDefense - finalDamage);

        Debug.Log($"적에게 {damageAfterDefense} 데미지! (적 HP: {enemyHP}/{enemyMaxHP})");
    }

    /// <summary>
    /// 플레이어에게 방어력을 추가합니다.
    /// </summary>
    public void AddDefense(int amount) {
        playerDefense += amount;
        Debug.Log($"방어력 +{amount} (현재: {playerDefense})");
    }

    /// <summary>
    /// 카드를 드로우합니다.
    /// </summary>
    public void DrawCards(int count) {
        for (int i = 0; i < count; i++) {
            if (deck.Count == 0) {
                // 덱이 비었으면 버리기 더미를 섞어서 덱으로
                if (discardPile.Count > 0) {
                    deck.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDeck();
                    Debug.Log("버리기 더미를 섞어서 덱으로 만들었습니다.");
                } else {
                    Debug.Log("덱과 버리기 더미가 모두 비어있어 카드를 뽑을 수 없습니다.");
                    return;
                }
            }

            if (deck.Count > 0) {
                Card drawnCard = deck[0];
                deck.RemoveAt(0);
                hand.Add(drawnCard);
                Debug.Log($"카드 드로우: {drawnCard.cardName}");
            }
        }
    }

    /// <summary>
    /// 플레이어에게 버프를 추가합니다.
    /// </summary>
    public void AddBuff(string statName, int amount) {
        switch (statName.ToLower()) {
            case "strength":
            case "힘":
                playerStrength += amount;
                Debug.Log($"힘 +{amount} (현재: {playerStrength})");
                break;
                // 나중에 다른 스탯 추가 가능
        }
    }

    /// <summary>
    /// 에너지를 추가합니다.
    /// </summary>
    public void AddEnergy(int amount) {
        playerEnergy += amount;
        Debug.Log($"에너지 +{amount} (현재: {playerEnergy})");
    }

    /// <summary>
    /// 덱을 섞습니다.
    /// </summary>
    private void ShuffleDeck() {
        for (int i = deck.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }
}