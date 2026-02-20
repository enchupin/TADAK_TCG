using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 데이터를 저장하는 ScriptableObject
/// JSON에서 변환되어 .asset 파일로 저장됩니다.
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public string description;  // 카드 설명
    
    [Header("강화")]
    public List<int> enforceCardIds = new List<int>();  // 강화 가능한 카드 ID 목록
    
    [Header("Addressables 주소")]
    public string artworkAddress;
    public string effectAddress;
    public string soundAddress;
    
    [Header("효과")]
    public List<CardEffectData> effects = new List<CardEffectData>();
    
    /// <summary>
    /// ScriptableObject → Card 객체 변환
    /// </summary>
    public Card ToCard()
    {
        Card card = new Card
        {
            cardId = this.cardId,
            cardName = this.cardName,
            character = this.character,
            cost = this.cost,
            description = this.description,
            enforceCardIds = new List<int>(this.enforceCardIds),
            artworkAddress = this.artworkAddress,
            effectAddress = this.effectAddress,
            soundAddress = this.soundAddress,
            effects = new List<ICardEffect>()
        };
        
        // 효과 변환
        foreach (var effectData in effects)
        {
            ICardEffect effect = effectData.CreateEffect();
            if (effect != null)
                card.effects.Add(effect);
        }
        
        return card;
    }
}
