using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 카드 데이터를 관리하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CardCollection", menuName = "TCG/Card Collection")]
public class CardCollection : ScriptableObject
{
    [Header("모든 카드")]
    public List<CardData> allCards = new();
    
    /// <summary>
    /// 모든 CardData를 Card 객체로 변환
    /// </summary>
    public List<Card> GetAllCards() {
        List<Card> cards = new();
        foreach (var cardData in allCards)
        {
            cards.Add(cardData.ToCard());
        }
        return cards;
    }
    
    /// <summary>
    /// ID로 카드 찾기
    /// </summary>
    public CardData GetCardById(int cardId) {
        return allCards.Find(c => c.cardId == cardId);
    }
    
    /// <summary>
    /// ID 리스트로 Card 객체 리스트 생성
    /// </summary>
    public List<Card> GetCardsByIds(List<int> cardIds) {
        List<Card> cards = new();
        foreach (int id in cardIds) {
            CardData cardData = GetCardById(id);
            if (cardData != null) {
                cards.Add(cardData.ToCard());
            }
            else {
                Debug.LogWarning($"Card not found: {id}");
            }
        }
        return cards;
    }
}
