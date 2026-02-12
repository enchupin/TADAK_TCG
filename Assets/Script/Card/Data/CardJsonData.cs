using System;
using System.Collections.Generic;

/// <summary>
/// JSON 직렬화를 위한 데이터 클래스들
/// </summary>

[Serializable]
public class CardDataList
{
    public List<CardJsonData> cards;
}

[Serializable]
public class CardJsonData
{
    public int cardId;
    public string name;
    public int characterId;
    public int cost;
    public string description;
    public List<string> keywords;
    public List<int> enforce;
    public AddressablesData addressables;
    public List<EffectJsonData> effects;
}


[Serializable]
public class AddressablesData
{
    public string artwork;
    public string effect;
    public string sound;
}

[Serializable]
public class EffectJsonData
{
    public string type;
    
    // Common
    public int amount; // or string based formula, handled by custom parser
    public string target; // "SingleEnemy", "AllEnemies", "Self", etc.
    
    // DamagePerCardPlayedEffect
    public int baseDamage;
    public int bonusPerCard;
    
    // ExecuteDamageEffect
    public float hpThreshold;
    public float multiplier;
    
    // BuffEffect
    public string stat; // Legacy?
    public int buffId;  // New
    public string buffType; 
    public int duration;
    
    // KeywordEffect
    public string keyword;  // "보존", "휘발" 등
    
    // Generator Effects
    public List<RandomCardData> RandomCard;
    public List<int> cardId; // For ChoiceGenerate or specific card lists
    
    // New Fields for Complex Effects
    public int count; // For Repeat, DiscardHand, ExhaustHand
    
    // Nested Effects (Recursive)
    public EffectJsonData effect; // Single nested effect (e.g. for ConsumeDefense?)
    public List<EffectJsonData> effects; // List of nested effects (e.g. for Repeat)
    
    // Conditional & Reactive
    public ConditionJsonData condition;
    public EffectJsonData onAction; // 반응형 효과
    
    // Legacy support fields (if needed)
    public EffectJsonData successEffect;
    public EffectJsonData failEffect;
}

[Serializable]
public class ConditionJsonData
{
    public string mode; // "And", "Or"
    public List<CheckJsonData> checks;
    public EffectJsonData successEffect;
    public EffectJsonData failEffect;
}

[Serializable]
public class CheckJsonData
{
    public string subject;   // "Source", "Target", "EventValue"
    public string property;  // "Hp", "Cost", "BuffId"...
    public string param;     // Optional param (BuffId etc)
    public string @operator; // "Eq", "Gt", "In"...
    public string value;     // Value to compare
}

[Serializable]
public class RandomCardData
{
    public int cardId;
    public int weight;
}
