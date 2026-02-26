using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Evaluates card conditions.
/// </summary>
public static class ConditionEvaluator
{
    // Legacy string condition support.
    public static bool Evaluate(string condition, string value, TrainingBattleManager battleManager)
    {
        if (string.IsNullOrEmpty(condition)) return true;
        return true;
    }

    /// <summary>
    /// Evaluates ConditionData checks with And/Or mode.
    /// </summary>
    public static bool Evaluate(ConditionData data, TrainingBattleManager battleManager)
    {
        if (data == null || data.checks == null || data.checks.Count == 0) return true;

        bool result = (data.mode == "Or") ? false : true;

        foreach (CheckData check in data.checks)
        {
            bool isCheckMet = EvaluateCheck(check, battleManager);

            if (data.mode == "Or")
            {
                if (isCheckMet) return true;
            }
            else
            {
                if (!isCheckMet) return false;
            }
        }

        return result;
    }

    private static bool EvaluateCheck(CheckData check, TrainingBattleManager battleManager)
    {
        if (check == null || battleManager == null) return false;

        // Requested support:
        // { "subject": "EnemyCount", "operator": "Eq", "value": 1 }
        string op = string.IsNullOrEmpty(check.@operator) ? "Eq" : check.@operator;

        if (check.subject == "EnemyCount" || check.property == "EnemyCount")
        {
            int enemyCount = 0;
            List<Monster> livingMonsters = battleManager.GetLivingMonsters();
            if (livingMonsters != null)
            {
                enemyCount = livingMonsters.Count;
            }
            else if (battleManager.spawnedMonsters != null)
            {
                foreach (Monster monster in battleManager.spawnedMonsters)
                {
                    if (monster != null && !monster.IsDead())
                    {
                        enemyCount++;
                    }
                }
            }

            return Compare(enemyCount, op, check.value);
        }

        object subjectObj = GetSubject(check.subject, battleManager);
        if (subjectObj == null) return false;

        float subjectValue = GetPropertyValue(subjectObj, check.property, check.param);
        return Compare(subjectValue, op, check.value);
    }

    private static object GetSubject(string subjectType, TrainingBattleManager bm)
    {
        switch (subjectType)
        {
            case "Source":
                return bm.playerData;
            case "Target":
                return UnityEngine.Object.FindFirstObjectByType<Monster>();
            case "Hand":
                return bm.handManager;
            default:
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
                case "HpLostTurn": return player.hpLostThisTurn;
                case "HasLostHpThisTurn": return player.hasLostHpThisTurn ? 1f : 0f;
                case "Defense": return player.defense;
                case "Energy": return player.energy;
                case "Buff":
                    int playerBuffId = int.Parse(param);
                    Buff playerBuff = player.currentBuffs.Find(b => b.data.buffId == playerBuffId);
                    return playerBuff != null ? playerBuff.stack : 0;
            }
        }
        else if (subject is Monster monster)
        {
            switch (property)
            {
                case "Hp": return monster.hp;
                case "Defense": return monster.defense;
                case "Buff":
                    int monsterBuffId = int.Parse(param);
                    Buff monsterBuff = monster.currentBuffs.Find(b => b.data.buffId == monsterBuffId);
                    return monsterBuff != null ? monsterBuff.stack : 0;
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
        if (bool.TryParse(targetStr, out bool targetBool))
        {
            bool actualBool = !Mathf.Approximately(actual, 0f);
            switch (op)
            {
                case "Eq": return actualBool == targetBool;
                case "Neq": return actualBool != targetBool;
                default:
                    float boolTarget = targetBool ? 1f : 0f;
                    return Compare(actual, op, boolTarget.ToString());
            }
        }

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
