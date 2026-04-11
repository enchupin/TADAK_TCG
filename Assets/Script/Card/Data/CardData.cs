using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 데이터를 저장하는 ScriptableObject
/// JSON에서 변환된 .asset 파일로 저장
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "TCG/Card Data")]
public class CardData : ScriptableObject
{
    [Header("기본 정보")]
    public int cardId;
    public string cardName;
    public Character character;
    public int cost;
    public CardCostType costType = CardCostType.Energy;
    public string description;
    
    [Header("강화")]
    public List<int> enforceCardIds = new();  // 강화 가능한 카드 ID 목록

    [Header("키워드")]
    public List<int> keywords = new();
    
    [Header("효과")]
    public List<CardEffectData> effects = new();

    [Header("드로우 시 효과")]
    public List<CardEffectData> onDrawEffects = new();

    [Header("턴 종료 손패 효과")]
    public List<CardEffectData> endTurnInHandEffects = new();
    
    /// <summary>
    /// ScriptableObject를 Card 객체로 변환
    /// </summary>
    public Card ToCard()
    {
        Card card = new()
        {
            cardId = cardId,
            cardName = cardName,
            character = character,
            cost = cost,
            costType = costType,
            description = description,
            enforceCardIds = new List<int>(enforceCardIds),
            keywords = keywords != null ? new List<int>(keywords) : new List<int>(),
            effects = new List<ICardEffect>(),
            onDrawEffects = new List<ICardEffect>(),
            keepEffects = new List<ICardEffect>(),
            endTurnInHandEffects = new List<ICardEffect>()
        };

        card.effects = CardEffectFactory.CreateEffects(effects);
        card.onDrawEffects = CardEffectFactory.CreateEffects(onDrawEffects);
        card.keepEffects = CardEffectFactory.CreateKeepEffects(effects);
        card.endTurnInHandEffects = CardEffectFactory.CreateEffects(endTurnInHandEffects);
        card.InitializeRuntimeState();

        return card;
    }
}
