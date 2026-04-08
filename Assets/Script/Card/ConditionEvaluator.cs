using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Evaluates card conditions.
/// </summary>
public static class ConditionEvaluator
{
    /// <summary>
    /// Evaluates ConditionData checks.
    /// </summary>
    public static bool Evaluate(ConditionData data, TrainingBattleManager battleManager)
    {
        if (data == null || data.checks == null || data.checks.Count == 0) return true;

        foreach (CheckData check in data.checks)
        {
            bool isCheckMet = EvaluateCheck(check, battleManager);
            if (!isCheckMet) return false;
        }

        return true;
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

        if (check.subject == "Barrier" && string.IsNullOrWhiteSpace(check.property))
        {
            int barrier = battleManager.playerData != null ? battleManager.playerData.defense : 0;
            return Compare(barrier, op, check.value);
        }

        object subjectObj = GetSubject(check.subject, battleManager);
        if (subjectObj == null) return false;

        float subjectValue = GetPropertyValue(subjectObj, check.property, check.param);
        return Compare(subjectValue, op, check.value);
    }

    private static object GetSubject(string subjectType, TrainingBattleManager bm)
    {
        if (bm?.battleContext != null && !string.IsNullOrWhiteSpace(subjectType)) {
            Card contextCard = bm.battleContext.GetContextCard(subjectType);
            if (contextCard != null) {
                return contextCard;
            }
        }

        switch (subjectType)
        {
            case "Self":
                Card selfCard = bm?.battleContext?.GetLastPlayedCard();
                if (selfCard != null) {
                    return selfCard;
                }
                return bm?.playerData;
            case "Source":
                return bm.playerData;
            case "Target":
                if (bm.currentTarget != null && !bm.currentTarget.IsDead()) {
                    return bm.currentTarget;
                }

                List<Monster> livingMonsters = bm.GetLivingMonsters();
                if (livingMonsters != null && livingMonsters.Count == 1) {
                    return livingMonsters[0];
                }

                return null;
            case "Hand":
                return bm.handManager;
            default:
                return bm.playerData;
        }
    }

    private static float GetPropertyValue(object subject, string property, string param)
    {
        if (subject is Card card)
        {
            switch (property)
            {
                case "CardId": return card.cardId;
                case "Cost": return card.cost;
                case "CharacterId": return (int)card.character;
                case "IsPotion": return IsPotionCard(card) ? 1f : 0f;
                case "HasKeyword":
                    return card.HasKeyword(ResolveKeywordId(param)) ? 1f : 0f;
            }
        }

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
                    return player.GetBuffStack(playerBuffId);
                case "HasBuff":
                    int playerHasBuffId = int.Parse(param);
                    return player.GetBuffStack(playerHasBuffId) > 0 ? 1f : 0f;
            }
        }
        else if (subject is Monster monster)
        {
            switch (property)
            {
                case "Hp": return monster.hp;
                case "Defense": return monster.defense;
                case "HasAttackIntent": return monster.HasAttackIntent ? 1f : 0f;
                case "Buff":
                    int monsterBuffId = int.Parse(param);
                    return monster.GetBuffStack(monsterBuffId);
                case "HasBuff":
                    int monsterHasBuffId = int.Parse(param);
                    return monster.GetBuffStack(monsterHasBuffId) > 0 ? 1f : 0f;
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

    private static bool IsPotionCard(Card card)
    {
        if (card == null) return false;
        if (card.character != Character.Isla) return false;

        return card.cardId >= 101080 && card.cardId <= 101087;
    }

    private static int ResolveKeywordId(string rawKeyword)
    {
        if (int.TryParse(rawKeyword, out int keywordId))
        {
            return keywordId;
        }

        switch (rawKeyword)
        {
            case "Keep": return CardKeywordIds.Keep;
            case "Unplayable": return CardKeywordIds.Unplayable;
            case "Exhaust": return CardKeywordIds.Exhaust;
            case "Power": return CardKeywordIds.Power;
            case "Opening": return CardKeywordIds.Opening;
            case "Shadow": return CardKeywordIds.Shadow;
            case "Finale": return CardKeywordIds.Finale;
            case "Ghost": return CardKeywordIds.Ghost;
            case "Unique": return CardKeywordIds.Unique;
            default: return 0;
        }
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
