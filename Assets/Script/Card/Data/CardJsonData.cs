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
    public List<string> keywords; // 삭제 예정
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
    // 예: "Attack", "Draw" 등
    // (일부 JSON은 type 없이 condition만 둘 수 있으며, 그 경우 컨버터에서 Conditional로 추론함)
    public string type;

    // onAction/중첩 효과에서 "이 효과가 어떤 데이터를 전달받아 실행되는지"를 명시하는 키.
    // 예: "SelectedCard", "DrawnCard", "LostBarrier", "UnblockedDamage" 등
    // 주의: 이 필드는 현재 DTO 레벨에서 값을 보존하는 목적이며,
    // 실제 런타임 소비 방식은 컨버터/효과 실행 로직에서 추가로 처리해야 함.
    public string subject;

    public int amount;
    // amountFormula는 수식 기반 값 계산용 문자열.
    // 예: "discarded * 2", "finalDamage", "*1.5"
    public string amountFormula;
    public string target;

    public string stat;
    public int buffId;
    public int duration;
    public List<int> cardId;
    public string cardIdGroup;

    public int count;

    // effects는 Repeat/Conditional 성공 분기처럼 다중 하위 효과가 필요한 경우 사용한다.
    // 단일 하위 효과도 리스트 1개 원소로 동일하게 표현할 수 있다.
    public List<EffectJsonData> effects;

    public ConditionJsonData condition;
    // 현재 효과가 끝난 뒤 연쇄적으로 실행할 후속 효과.
    // subject가 함께 정의되면 "어떤 데이터 문맥으로 후속 효과를 실행할지"를 명확히 표현 가능.
    public EffectJsonData onAction;
}

[Serializable]
public class ConditionJsonData
{
    public string mode;
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
