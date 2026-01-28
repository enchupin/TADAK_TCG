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
    public string rarity; // Optional, might not be in choleCards
    public string description;
    public List<string> keywords;
    public List<int> enforce;
    public AddressablesData addressables;
    public List<EffectJsonData> effects;
    
    /// <summary>
    /// characterId를 Character enum으로 변환
    /// </summary>
    public static Character GetCharacterFromId(int characterId)
    {
        switch (characterId)
        {
            case 101: return Character.Chloe;
            case 201: return Character.Ignia;
            case 301: return Character.Declan;
            case 102: return Character.Archer;
            case 103: return Character.Knight;
            default:
                UnityEngine.Debug.LogWarning($"Unknown characterId: {characterId}, defaulting to Chloe");
                return Character.Chloe;
        }
    }
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
    public int amount;
    public string target; // "SingleEnemy", "AllEnemies", "Self", etc.
    
    // DamagePerCardPlayedEffect
    public int baseDamage;
    public int bonusPerCard;
    
    // ExecuteDamageEffect
    public float hpThreshold;
    public float multiplier;
    
    // BuffEffect
    public string stat; // Legacy?
    public string buffType; // New field in choleCards
    public int duration;
    
    // Generator Effects
    public List<RandomCardData> RandomCard;
    public List<int> cardId; // For ChoiceGenerate
}

[Serializable]
public class RandomCardData
{
    public int cardId;
    public int weight;
}
