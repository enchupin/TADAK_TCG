using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 조건 데이터를 평가하는 정적 클래스
/// Subject(대상) -> Property(속성) -> Operator(비교) 구조를 지원합니다.
/// </summary>
public static class ConditionEvaluator
{
    // 레거시 지원용
    public static bool Evaluate(string condition, string value, TrainingBattleManager battleManager)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        // 기존 단순 문자열 조건 처리 로직 유지 또는 ConditionData로 변환하여 처리
        // 여기서는 간단히 기존 로직 유지
        return true; 
    }

    /// <summary>
    /// 새로운 ConditionData 구조체 평가
    /// </summary>
    public static bool Evaluate(ConditionData data, TrainingBattleManager battleManager)
    {
        if (data == null || data.checks == null || data.checks.Count == 0) return true;

        bool result = (data.mode == "Or") ? false : true;

        foreach (var check in data.checks)
        {
            bool isCheckMet = EvaluateCheck(check, battleManager);

            if (data.mode == "Or")
            {
                if (isCheckMet) return true; // 하나라도 만족하면 True
            }
            else // And
            {
                if (!isCheckMet) return false; // 하나라도 불만족하면 False
            }
        }

        return result;
    }

    private static bool EvaluateCheck(CheckData check, TrainingBattleManager battleManager)
    {
        // 1. 대상(Subject) 결정
        object subjectObj = GetSubject(check.subject, battleManager);
        if (subjectObj == null) return false;

        // 2. 속성(Property) 값 추출
        float subjectValue = GetPropertyValue(subjectObj, check.property, check.param);

        // 3. 비교(Operator)
        return Compare(subjectValue, check.@operator, check.value);
    }

    private static object GetSubject(string subjectType, TrainingBattleManager bm)
    {
        switch (subjectType)
        {
            case "Source": // 플레이어 (또는 시전 주체)
                return bm.playerData;
            case "Target": // 타겟 (적) - 단일 타겟 기준
                return bm.monster; 
            case "Hand":
                return bm.handManager;
            default:
                // EventValue 등 추가 컨텍스트가 필요한 경우 여기서 처리 불가할 수 있음
                // 필요한 경우 메서드 시그니처 수정 필요
                return bm.playerData;
        }
    }

    private static float GetPropertyValue(object subject, string property, string param)
    {
        if (subject is PlayerData player)
        {
            switch (property)
            {
                case "Hp": return player.hp;
                case "Defense": return player.defense;
                case "Energy": return player.energy;
                case "Buff": 
                    int buffId = int.Parse(param);
                    var buff = player.currentBuffs.Find(b => b.data.buffId == buffId);
                    return buff != null ? buff.stack : 0;
            }
        }
        else if (subject is Monster monster)
        {
            switch (property)
            {
                case "Hp": return monster.hp;
                case "Defense": return monster.defense;
                case "Buff":
                    int buffId = int.Parse(param);
                    var buff = monster.currentBuffs.Find(b => b.data.buffId == buffId);
                    return buff != null ? buff.stack : 0;
                 // Intent 등 추가 가능
            }
        }
        else if (subject is HandManager hand)
        {
            switch (property)
            {
                case "Count": return hand.GetHandCount();
            }
        }

        return 0f;
    }

    private static bool Compare(float actual, string op, string targetStr)
    {
        float target = 0f;
        float.TryParse(targetStr, out target);

        switch (op)
        {
            case "Eq": return Mathf.Approximately(actual, target);
            case "Neq": return !Mathf.Approximately(actual, target);
            case "Gt": return actual > target;
            case "Gte": return actual >= target;
            case "Lt": return actual < target;
            case "Lte": return actual <= target;
            default: return false;
        }
    }
}
