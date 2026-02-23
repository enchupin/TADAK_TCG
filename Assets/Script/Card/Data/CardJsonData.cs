using System;
using System.Collections.Generic;

/// <summary>
/// JSON deserialization DTOs for card conversion.
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

    public int amount;
    public string amountFormula;
    public string getAmount;
    public string GetAmount;
    public string getamount;
    public string target;

    public int baseDamage;
    public int bonusPerCard;

    public float hpThreshold;
    public float multiplier;

    public string stat;
    public int buffId;
    public string buffType;
    public int duration;

    public string keyword;

    public List<RandomCardData> RandomCard;
    public List<int> cardId;

    public int count;

    public EffectJsonData effect;
    public List<EffectJsonData> effects;

    public ConditionJsonData condition;
    public EffectJsonData onAction;

    public EffectJsonData successEffect;
    public EffectJsonData failEffect;
}

[Serializable]
public class ConditionJsonData
{
    public string mode;
    public List<CheckJsonData> checks;
    public EffectJsonData successEffect;
    public EffectJsonData failEffect;
}

[Serializable]
public class CheckJsonData
{
    public string subject;
    public string property;
    public string param;
    public string @operator;
    public string value;
}

[Serializable]
public class RandomCardData
{
    public int cardId;
    public int weight;
}
