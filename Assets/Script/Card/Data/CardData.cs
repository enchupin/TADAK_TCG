using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 데이터를 저장하는 ScriptableObject
/// JSON에서 변환되어 .asset 파일로 저장
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public string description;
    
    [Header("강화")]
    public List<int> enforceCardIds = new();  // 강화 가능한 카드 ID 목록
    
    [Header("효과")]
    public List<CardEffectData> effects = new();
    
    /// <summary>
    /// ScriptableObject → Card 객체 변환
    /// </summary>
    public Card ToCard()
    {
        Card card = new() {
            cardId = this.cardId,
            cardName = this.cardName,
            character = this.character,
            cost = this.cost,
            description = this.description,
            enforceCardIds = new List<int>(this.enforceCardIds),
            effects = new List<ICardEffect>()
        };
        
        // 효과 변환
        card.effects = CardEffectFactory.CreateEffects(effects);
        
        return card;
    }
}
