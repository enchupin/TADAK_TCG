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

/// <summary>
/// 효과 데이터 (Serializable)
/// Inspector에서 편집 가능
/// </summary>
[System.Serializable]
public class CardEffectData
{
    public EffectType type;
    
    [Header("공통")]
    public int amount;  // 고정값 (하위 호환)
    public string amountFormula;  // 수식 문자열 ("UseCardInCombat * 3" 등)
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
    
    [Header("중첩 효과 (ConsumeDefense 등)")]
    public CardEffectData nestedEffect;  // 중첩 효과
    
    [Header("Keyword")]
    public string keyword;  // "보존", "휘발" 등
    
    [Header("GenerateCard")]
    public System.Collections.Generic.List<RandomCardData> RandomCard;  // 랜덤 카드 생성
    
    /// <summary>
    /// CardEffectData → ICardEffect 변환
    /// </summary>
    public ICardEffect CreateEffect()
    {
        switch (type)
        {
            case EffectType.Damage:
                return new DamageEffect { 
                    amount = amount, 
                    amountFormula = amountFormula,
                    target = target 
                };
            case EffectType.Defense:
                return new DefenseEffect { 
                    amount = amount,
                    amountFormula = amountFormula
                };
            case EffectType.Draw:
                return new DrawEffect { 
                    amount = amount,
                    amountFormula = amountFormula
                };
            case EffectType.Buff:
                return new BuffEffect { 
                    stat = stat, 
                    amount = amount,
                    amountFormula = amountFormula,
                    duration = duration 
                };
            case EffectType.Energy:
                return new EnergyEffect { 
                    amount = amount,
                    amountFormula = amountFormula
                };
            case EffectType.DamagePerCardPlayed:
                return new DamagePerCardPlayedEffect { 
                    baseDamage = baseDamage, 
                    bonusPerCard = bonusPerCard 
                };
            case EffectType.Execute:
                return new ExecuteDamageEffect { 
                    baseDamage = baseDamage, 
                    hpThreshold = hpThreshold, 
                    multiplier = multiplier 
                };
            case EffectType.Heal:
                return new HealEffect {
                    amount = amount,
                    amountFormula = amountFormula,
                    target = target
                };
            case EffectType.MultiplyDefense:
                return new MultiplyDefenseEffect {
                    amount = amount,
                    amountFormula = amountFormula
                };
            case EffectType.ConsumeDefense:
                return new ConsumeDefenseEffect {
                    amount = amount,
                    amountFormula = amountFormula,
                    nestedEffect = nestedEffect?.CreateEffect()
                };
            case EffectType.GenerateCard:
                return new GenerateCardEffect {
                    RandomCard = RandomCard
                };
            case EffectType.Keyword:
                return new KeywordEffect {
                    keyword = keyword,
                    amount = amount,
                    amountFormula = amountFormula
                };
            case EffectType.ChoiceHand:
            case EffectType.ChoiceDiscard:
            case EffectType.Conditional:
            case EffectType.RandomGenerate:
                // TODO: 구현 예정
                Debug.LogWarning($"[CardData] Effect type {type} not implemented yet");
                return null;
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
    Execute,
    Heal,
    MultiplyDefense,
    ConsumeDefense,
    GenerateCard,
    Keyword,
    ChoiceHand,      // TODO: 구현 예정
    ChoiceDiscard,   // TODO: 구현 예정
    Conditional,     // TODO: 구현 예정
    RandomGenerate   // TODO: 구현 예정
}
