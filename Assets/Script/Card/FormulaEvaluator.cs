using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
/// <summary>
/// 수식 문자열을 평가하는 클래스
/// JSON의 "amount": "UseCardInCombat * 3" 같은 수식을 계산
/// </summary>
public static class FormulaEvaluator
{
    /// <summary>
    /// 문자열이 수식인지 확인
    /// </summary>
    public static bool IsFormula(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        // 숫자만 있으면 수식이 아님
        if (int.TryParse(value, out _)) return false;
        return true;
    }
    /// <summary>
    /// 수식 문자열을 평가하여 정수 반환
    /// </summary>
    public static int Evaluate(string formula, BattleContext context, PlayerData player = null, int baseValue = 0)
    {
        return Evaluate(formula, context, player, null, baseValue);
    }

    public static int Evaluate(string formula, BattleContext context, PlayerData player, List<int> cardIdFilter, int baseValue = 0)
    {
        if (string.IsNullOrEmpty(formula))
        {
            return 0;
        }

        if (int.TryParse(formula, out int result))
        {
            return result;
        }

        formula = formula.Replace(" ", "");

        if (formula.IndexOf("discarded", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError("[FormulaEvaluator] 'discarded' 수식 키워드는 더 이상 지원하지 않습니다. 'moved'를 사용하세요");
            return 0;
        }

        int cardsPlayedInCombat = context != null
            ? context.GetCardsPlayedThisCombatCount(cardIdFilter)
            : 0;
        int cardsPlayedInTurn = context != null
            ? context.GetCardsPlayedThisTurnCount(cardIdFilter)
            : 0;
        int hasLostHpThisTurn = player != null && player.hasLostHpThisTurn ? 1 : 0;

        switch (formula.ToLower())
        {
            case "all":
                return player != null ? player.defense : 0;

            case "usecardincombat":
                return cardsPlayedInCombat;

            case "usecardinturn":
                return cardsPlayedInTurn;

            case "consumed":
                return context != null ? context.defenseConsumed : 0;

            case "moved":
                return Mathf.Max(0, baseValue);

            case "exhausted":
                return context != null ? context.cardsExhaustedThisTurn : 0;

            case "cardsdrawnthisturn":
                return context != null ? context.cardsDrawnThisTurn : 0;

            case "finaldamage":
                return context != null ? context.lastDamageDealt : 0;

            case "value":
                return baseValue;

            case "haslosthpthisturn":
                return hasLostHpThisTurn;
        }

        try
        {
            string expression = formula;
            if (expression.StartsWith("*") || expression.StartsWith("/") || expression.StartsWith("+") || expression.StartsWith("-"))
            {
                expression = baseValue + expression;
            }
            expression = expression.Replace("UseCardInCombat", cardsPlayedInCombat.ToString());
            expression = expression.Replace("UseCardInTurn", cardsPlayedInTurn.ToString());
            expression = expression.Replace("consumed", (context != null ? context.defenseConsumed : 0).ToString());
            expression = expression.Replace("moved", Mathf.Max(0, baseValue).ToString());
            expression = expression.Replace("exhausted", (context != null ? context.cardsExhaustedThisTurn : 0).ToString());
            expression = expression.Replace("cardsDrawnThisTurn", (context != null ? context.cardsDrawnThisTurn : 0).ToString());
            expression = expression.Replace("finalDamage", (context != null ? context.lastDamageDealt : 0).ToString());
            expression = expression.Replace("value", baseValue.ToString());
            expression = expression.Replace("HasLostHpThisTurn", hasLostHpThisTurn.ToString());

            return EvaluateSimpleExpression(expression);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FormulaEvaluator] Failed to evaluate formula: {formula}, Error: {e.Message}");
            return 0;
        }
    }

    private static int EvaluateSimpleExpression(string expression)
    {
        while (expression.Contains("("))
        {
            int start = expression.LastIndexOf('(');
            int end = expression.IndexOf(')', start);
            if (end == -1) break;

            string subExpr = expression.Substring(start + 1, end - start - 1);
            int subResult = EvaluateSimpleExpression(subExpr);
            expression = expression.Substring(0, start) + subResult + expression.Substring(end + 1);
        }

        expression = EvaluateOperator(expression, '*');
        expression = EvaluateOperator(expression, '/');
        expression = EvaluateOperator(expression, '+');
        expression = EvaluateOperator(expression, '-');

        if (int.TryParse(expression, out int result))
        {
            return result;
        }

        if (float.TryParse(expression, out float floatResult))
        {
            return Mathf.RoundToInt(floatResult);
        }

        return 0;
    }

    private static string EvaluateOperator(string expression, char op)
    {
        string pattern = op == '*' || op == '/'
            ? @"([\d.]+)\s*\" + op + @"\s*([\d.]+)"
            : @"(^|[^.\d])([\d.]+)\s*\" + op + @"\s*([\d.]+)";

        while (Regex.IsMatch(expression, pattern))
        {
            Match match = Regex.Match(expression, pattern);

            float left, right;
            string leftStr, rightStr;

            if (op == '+' || op == '-')
            {
                leftStr = match.Groups[2].Value;
                rightStr = match.Groups[3].Value;
            }
            else
            {
                leftStr = match.Groups[1].Value;
                rightStr = match.Groups[2].Value;
            }

            if (!float.TryParse(leftStr, out left) || !float.TryParse(rightStr, out right))
            {
                break;
            }

            float result = op switch
            {
                '*' => left * right,
                '/' => right != 0 ? left / right : 0,
                '+' => left + right,
                '-' => left - right,
                _ => 0
            };

            expression = expression.Replace(match.Value, result.ToString());
        }

        return expression;
    }
}
