using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 데이터베이스
/// JSON에서 카드 데이터를 로드하고 Card 객체를 생성합니다.
/// </summary>
public class CardDatabase : MonoBehaviour
{
    [Header("JSON 파일 경로")]
    [SerializeField] private string jsonFileName = "cards";
    
    [Header("로드된 카드들")]
    public List<Card> allCards = new List<Card>();
    
    void Awake()  // ← Start에서 Awake로 변경!
    {
        LoadCardsFromJSON();
    }
    
    /// <summary>
    /// JSON 파일에서 카드 데이터를 로드합니다.
    /// </summary>
    public void LoadCardsFromJSON()
    {
        // Resources 폴더에서 JSON 파일 로드
        TextAsset jsonFile = Resources.Load<TextAsset>($"JsonData/{jsonFileName}");
        
        if (jsonFile == null)
        {
            Debug.LogError($"JSON 파일을 찾을 수 없습니다: Resources/JsonData/{jsonFileName}.json");
            return;
        }
        
        // JSON 파싱
        CardDataList cardDataList = JsonUtility.FromJson<CardDataList>(jsonFile.text);
        
        if (cardDataList == null || cardDataList.cards == null)
        {
            Debug.LogError("JSON 파싱 실패!");
            return;
        }
        
        Debug.Log($"JSON 로드 성공! 카드 {cardDataList.cards.Count}장 발견");
        
        // Card 객체 생성
        foreach (var cardData in cardDataList.cards)
        {
            Card card = CreateCardFromData(cardData);
            allCards.Add(card);
            
            Debug.Log($"카드 로드: [{card.cardId}] {card.cardName} (코스트: {card.cost}, 효과: {card.effects.Count}개)");
        }
        
        Debug.Log($"총 {allCards.Count}장의 카드가 로드되었습니다!");
    }
    
    /// <summary>
    /// JSON 데이터로부터 Card 객체를 생성합니다.
    /// </summary>
    private Card CreateCardFromData(CardJsonData data)
    {
        Card card = new Card();
        
        // 기본 정보
        card.cardId = data.cardId;
        card.cardName = data.name;
        card.cost = data.cost;
        card.rarity = data.rarity;
        
        // Addressables 주소
        if (data.addressables != null)
        {
            card.artworkAddress = data.addressables.artwork;
            card.effectAddress = data.addressables.effect;
            card.soundAddress = data.addressables.sound;
        }
        
        // 효과 생성
        if (data.effects != null)
        {
            foreach (var effectData in data.effects)
            {
                ICardEffect effect = CreateEffect(effectData);
                if (effect != null)
                {
                    card.effects.Add(effect);
                }
            }
        }
        
        return card;
    }
    
    /// <summary>
    /// Effect Factory - JSON 데이터로부터 효과 객체를 생성합니다.
    /// </summary>
    private ICardEffect CreateEffect(EffectJsonData data)
    {
        switch (data.type)
        {
            case "Damage":
                return new DamageEffect
                {
                    amount = data.amount,
                    target = ParseTargetType(data.target)
                };
            
            case "Defense":
                return new DefenseEffect
                {
                    amount = data.amount
                };
            
            case "Draw":
                return new DrawEffect
                {
                    amount = data.amount
                };
            
            case "Buff":
                return new BuffEffect
                {
                    stat = data.stat,
                    amount = data.amount,
                    duration = data.duration
                };
            
            case "Energy":
                return new EnergyEffect
                {
                    amount = data.amount
                };
            
            case "DamagePerCardPlayed":
                return new DamagePerCardPlayedEffect
                {
                    baseDamage = data.baseDamage,
                    bonusPerCard = data.bonusPerCard
                };
            
            case "ExecuteDamage":
                return new ExecuteDamageEffect
                {
                    baseDamage = data.baseDamage,
                    hpThreshold = data.hpThreshold,
                    multiplier = data.multiplier
                };
            
            default:
                Debug.LogWarning($"알 수 없는 효과 타입: {data.type}");
                return null;
        }
    }
    
    /// <summary>
    /// 문자열을 TargetType으로 변환합니다.
    /// </summary>
    private TargetType ParseTargetType(string targetString)
    {
        switch (targetString)
        {
            case "SingleEnemy":
                return TargetType.SingleEnemy;
            case "AllEnemies":
                return TargetType.AllEnemies;
            case "Self":
                return TargetType.Self;
            case "AllAllies":
                return TargetType.AllAllies;
            default:
                return TargetType.SingleEnemy;
        }
    }
    
    /// <summary>
    /// 카드 ID로 카드를 찾습니다.
    /// </summary>
    public Card GetCardById(string cardId)
    {
        return allCards.Find(c => c.cardId == cardId);
    }
    
    /// <summary>
    /// 랜덤 카드를 반환합니다.
    /// </summary>
    public Card GetRandomCard()
    {
        if (allCards.Count == 0) return null;
        return allCards[Random.Range(0, allCards.Count)];
    }
}
