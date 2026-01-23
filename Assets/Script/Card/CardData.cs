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
    public int cost;
    public string rarity;  // "common", "uncommon", "rare", "epic", "legendary"
    
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
            cost = this.cost,
            rarity = this.rarity,
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

/// <summary>
/// 효과 데이터 (Serializable)
/// Inspector에서 편집 가능
/// </summary>
[System.Serializable]
public class CardEffectData
{
    public EffectType type;
    
    [Header("공통")]
    public int amount;
    public TargetType target;
    
    [Header("Buff")]
    public string stat;
    public int duration;
    
    [Header("DamagePerCardPlayed")]
    public int baseDamage;
    public int bonusPerCard;
    
    [Header("Execute")]
    public float hpThreshold;
    public float multiplier;
    
    /// <summary>
    /// CardEffectData → ICardEffect 변환
    /// </summary>
    public ICardEffect CreateEffect()
    {
        switch (type)
        {
            case EffectType.Damage:
                return new DamageEffect { amount = amount, target = target };
            case EffectType.Defense:
                return new DefenseEffect { amount = amount };
            case EffectType.Draw:
                return new DrawEffect { amount = amount };
            case EffectType.Buff:
                return new BuffEffect { stat = stat, amount = amount, duration = duration };
            case EffectType.Energy:
                return new EnergyEffect { amount = amount };
            case EffectType.DamagePerCardPlayed:
                return new DamagePerCardPlayedEffect { baseDamage = baseDamage, bonusPerCard = bonusPerCard };
            case EffectType.Execute:
                return new ExecuteDamageEffect { baseDamage = baseDamage, hpThreshold = hpThreshold, multiplier = multiplier };
            default:
                Debug.LogWarning($"Unknown effect type: {type}");
                return null;
        }
    }
}

/// <summary>
/// 효과 타입 Enum
/// </summary>
public enum EffectType
{
    Damage,
    Defense,
    Draw,
    Buff,
    Energy,
    DamagePerCardPlayed,
    Execute
}
