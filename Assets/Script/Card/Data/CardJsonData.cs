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
    public string cardId;
    public string name;
    public int cost;
    public string rarity;
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
    
    // DamageEffect
    public int amount;
    public string target; // "SingleEnemy", "AllEnemies"
    
    // DamagePerCardPlayedEffect
    public int baseDamage;
    public int bonusPerCard;
    
    // ExecuteDamageEffect
    public float hpThreshold;
    public float multiplier;
    
    // BuffEffect
    public string stat;
    public int duration;
}
