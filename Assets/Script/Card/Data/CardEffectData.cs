using UnityEngine;
using System.Collections.Generic;

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
    public int buffId; // New: JSON uses buffId
    public int duration;
    
    [Header("DamagePerCardPlayed")]
    public int baseDamage;
    public int bonusPerCard;
    
    [Header("Execute")]
    public float hpThreshold;
    public float multiplier;
    
    [Header("중첩/조건부 효과")]
    [SerializeReference] public CardEffectData nestedEffect;  // 중첩 효과
    [SerializeReference] public List<CardEffectData> subEffects; // 여러 개의 하위 효과
    [SerializeReference] public CardEffectData failEffect;    // 조건 불만족 시 효과
    
    [Header("Conditional (New Struct)")]
    public ConditionData conditionData; // 구조체 형태의 조건 데이터
    
    // [Legacy] 하위 호환을 위해 유지하거나 삭제
    public string condition;      
    public string conditionValue; 

    [Header("Reactive Effect")]
    [SerializeReference] public CardEffectData onAction; // 반응형 효과 (Action 실행 후 트리거)

    [Header("Targeting / Selection")]
    public List<int> cardIdList; // 특정 카드 ID 목록 (Generate, Choice 등)
    public int count; // 반복 횟수 등
    
    [Header("Keyword")]
    public string keyword;  // "보존", "휘발" 등
    public int keywordId;   // 키워드 ID
    
    [Header("GenerateCard")]
    public List<RandomCardData> RandomCard;  // 랜덤 카드 생성
    
    /// <summary>
    /// CardEffectData → ICardEffect 변환
    /// </summary>
    public ICardEffect CreateEffect()
    {
        switch (type)
        {
            // 중첩 효과 파싱
            case EffectType.Repeat:
                List<ICardEffect> repeatedEffects = new List<ICardEffect>();
                if (subEffects != null) {
                    foreach(var sub in subEffects) {
                        var eff = sub.CreateEffect();
                        if(eff != null) repeatedEffects.Add(eff);
                    }
                }
                return new RepeatEffect { count = count, countFormula = amountFormula, effectsToRepeat = repeatedEffects };



            case EffectType.Conditional:
                // 조건부 효과 생성
                // 기존 문자열 조건(condition)과 새로운 구조체 조건(conditionData) 모두 지원해야 할 수 있음
                // 여기서는 새로운 ConditionEvaluator를 사용하는 ConditionalEffect로 연결
                List<ICardEffect> success = new List<ICardEffect>();
                if (conditionData.successEffect != null) { // 신규 구조
                     // 단일 효과지만 리스트로 처리 (편의상)
                     // 실제로는 conditionData.successEffect가 CardEffectData 타입이므로 재귀 호출
                     var eff = conditionData.successEffect.CreateEffect();
                     if (eff != null) success.Add(eff);
                } else if (subEffects != null) { // 기존 구조 호환
                    foreach(var sub in subEffects) {
                        var eff = sub.CreateEffect();
                        if(eff != null) success.Add(eff);
                    }
                }
                
                List<ICardEffect> fail = new List<ICardEffect>();
                if (conditionData.failEffect != null) { // 신규 구조
                     var eff = conditionData.failEffect.CreateEffect();
                     if (eff != null) fail.Add(eff);
                } else if (failEffect != null) { // 기존 구조 호환
                    var eff = failEffect.CreateEffect();
                    if (eff != null) fail.Add(eff);
                }
                return new ConditionalEffect { conditionData = this.conditionData, successEffects = success, failEffects = fail };

            case EffectType.Attack: // ongoing
                return new AttackEffect { amount = amount, amountFormula = amountFormula, target = target };
            case EffectType.Barrier: // clear
                return new BarrierEffect { amount = amount, amountFormula = amountFormula, target = target };
            case EffectType.DiscardHand:
                return new DiscardHandEffect { count = count, target = target };
            case EffectType.ExhaustHand:
                return new ExhaustHandEffect { count = count, target = target };
            case EffectType.Scry:
                return new ScryEffect { count = count };
            case EffectType.ChoiceHand:
                return new ChoiceHandEffect { count = count };
            case EffectType.SelectCard:
                 return new SelectCardEffect { count = count, target = target };
            case EffectType.Damage:
                return new DamageEffect { amount = amount, amountFormula = amountFormula, target = target };
            case EffectType.Draw: // clear
                return new DrawEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.Buff:
                return new BuffEffect { stat = stat, buffId = buffId, amount = amount, amountFormula = amountFormula, duration = duration, target = target };
            case EffectType.Energy:
                return new EnergyEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.DamagePerCardPlayed:
                return new DamagePerCardPlayedEffect { baseDamage = baseDamage, bonusPerCard = bonusPerCard };
            case EffectType.Execute:
                return new ExecuteDamageEffect { baseDamage = baseDamage, hpThreshold = hpThreshold, multiplier = multiplier };
            case EffectType.Heal:
                return new HealEffect { amount = amount, amountFormula = amountFormula, target = target };
            case EffectType.MultiplyDefense:
                return new MultiplyDefenseEffect { amount = amount, amountFormula = amountFormula };
            case EffectType.ConsumeDefense:
                return new ConsumeDefenseEffect { amount = amount, amountFormula = amountFormula, nestedEffect = nestedEffect?.CreateEffect() };
            case EffectType.GenerateCard:
                return new GenerateCardEffect { RandomCard = RandomCard, cardIdList = cardIdList, target = target };
            case EffectType.Keyword:
                return new KeywordEffect { keyword = keyword, amount = amount, amountFormula = amountFormula };
            case EffectType.ChoiceDiscard: // count or amount based on field usage
                return new ChoiceDiscardEffect {  amount = count,effect = nestedEffect?.CreateEffect() };
            case EffectType.Pickup:
                return new PickupEffect();
            case EffectType.RandomGenerate:
                Debug.LogWarning($"[CardData] 아직 구현되지 않은 효과 타입: {type}");
                return null;
            default:
                Debug.LogWarning($"알 수 없는 효과 타입: {type}");
                return null;
        }
    }
}

/// <summary>
/// 조건 데이터 (And/Or 그룹)
/// </summary>
[System.Serializable]
public class ConditionData
{
    public string mode = "And"; // "And", "Or"
    public List<CheckData> checks = new List<CheckData>();
    [SerializeReference] public CardEffectData successEffect; // 조건 만족 시 실행할 효과
    [SerializeReference] public CardEffectData failEffect;    // 조건 불만족 시 실행할 효과
}

/// <summary>
/// 개별 조건 검사 데이터
/// Subject -> Property -> Operator -> Value 흐름으로 검사
/// </summary>
[System.Serializable]
public class CheckData
{
    public string subject;   // "Source", "Target", "EventValue"
    public string property;  // "Hp", "Cost", "BuffId" 등
    public string param;     // 추가 인자 (BuffId 등)
    public string @operator; // "Eq", "Gt", "In", "InRange"...
    public string value;     // 비교 값 (문자열로 저장 후 파싱)
}

/// <summary>
/// 효과 타입 Enum
/// </summary>
public enum EffectType
{
    Damage,
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
    Attack,
    Barrier,
    ChoiceHand,
    ChoiceDiscard,
    Conditional,
    RandomGenerate,
    DiscardHand,
    ExhaustHand,
    SelectCard,
    Copy,
    Repeat,
    Scry,
    RemoveBuff,
    ConsumeBarrier,
    MultiplyBarrier,
    UseCard,
    Evade,
    Multiply,
    Increase,
    Cancel,
    Pickup
}
