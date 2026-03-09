using System;
using System.Collections.Generic;

/// <summary>
/// JSON deserialization DTOs for card conversion.
/// </summary>


[Serializable]
public class CardJsonData
{
    public int cardId;
    public string name;
    public int characterId;
    public int cost;
    public string description;
    public List<int> keywords; // 삭제 예정
    public string enforceGroup; // 연결 예정
    public AddressablesData addressables; // 삭제 예정
    public List<EffectJsonData> effects;
}

// 삭제 예정
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
    // 효과 타입 문자열
    public string type;

    // onAction/중첩 효과에서 "이 효과가 어떤 데이터를 전달받아 실행되는지"를 명시하는 키
    public string subject;

    // 
    public int amount;

    // amountFormula는 수식 기반 값 계산용 문자열
    public string amountFormula;

    public string target;

    public string stat;

    public int buffId;

    public int duration;

    public List<int> cardId;

    public string cardIdGroup;

    public int count;

    // effects는 Repeat/Conditional 성공 분기처럼 다중 하위 효과가 필요한 경우 사용
    public List<EffectJsonData> effects;

    public ConditionJsonData condition;

    // 현재 효과가 끝난 뒤 연쇄적으로 실행할 후속 효과
    // subject가 함께 정의되면 "어떤 데이터 문맥으로 후속 효과를 실행할지"를 명확히 표현 가능
    public List<EffectJsonData> onAction;
}

[Serializable]
public class ConditionJsonData
{
    public List<CheckJsonData> checks;
    public List<EffectJsonData> effects;
    public List<EffectJsonData> elseEffects;
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

