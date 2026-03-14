using System;
using System.Collections.Generic;
using System.Globalization;
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

        formula = formula.Replace(" ", string.Empty);

        if (formula.IndexOf("discarded", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError("[FormulaEvaluator] 'discarded' 수식 키워드는 더 이상 지원하지 않습니다. 'moved'를 사용하세요");
            return 0;
        }

        int cardsPlayedInCombat = context != null ? context.GetCardsPlayedThisCombatCount(cardIdFilter) : 0;
        int cardsPlayedInTurn = context != null ? context.GetCardsPlayedThisTurnCount(cardIdFilter) : 0;
        int hasLostHpThisTurn = player != null && player.hasLostHpThisTurn ? 1 : 0;

        switch (formula.ToLowerInvariant())
        {
            case "all":
                return player != null ? player.defense : 0;
            case "usecardincombat":
                return cardsPlayedInCombat;
            case "usecardinturn":
                return cardsPlayedInTurn;
            case "consumed":
                return context != null ? context.defenseConsumed : 0;
            case "shufflecount":
                return context != null ? context.deckShuffleCountThisCombat : 0;
            case "moved":
            case "value":
            case "eventvalue":
            case "unblockeddamage":
                return Mathf.Max(0, baseValue);
            case "exhausted":
                return context != null ? context.cardsExhaustedThisTurn : 0;
            case "cardsdrawnthisturn":
                return context != null ? context.cardsDrawnThisTurn : 0;
            case "finaldamage":
                return context != null ? context.lastDamageDealt : 0;
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

            expression = ReplaceFunctionCalls(expression, player);
            expression = ReplaceKnownKeywords(expression, context, cardsPlayedInCombat, cardsPlayedInTurn, hasLostHpThisTurn, baseValue);
            expression = EvaluateMinFunctions(expression);

            return EvaluateSimpleExpression(expression);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FormulaEvaluator] Failed to evaluate formula: {formula}, Error: {e.Message}");
            return 0;
        }
    }

    private static string ReplaceKnownKeywords(string expression, BattleContext context, int cardsPlayedInCombat, int cardsPlayedInTurn, int hasLostHpThisTurn, int baseValue)
    {
        expression = expression.Replace("UseCardInCombat", cardsPlayedInCombat.ToString());
        expression = expression.Replace("UseCardInTurn", cardsPlayedInTurn.ToString());
        expression = expression.Replace("consumed", (context != null ? context.defenseConsumed : 0).ToString());
        expression = expression.Replace("ShuffleCount", (context != null ? context.deckShuffleCountThisCombat : 0).ToString());
        expression = expression.Replace("moved", Mathf.Max(0, baseValue).ToString());
        expression = expression.Replace("exhausted", (context != null ? context.cardsExhaustedThisTurn : 0).ToString());
        expression = expression.Replace("cardsDrawnThisTurn", (context != null ? context.cardsDrawnThisTurn : 0).ToString());
        expression = expression.Replace("finalDamage", (context != null ? context.lastDamageDealt : 0).ToString());
        expression = expression.Replace("value", baseValue.ToString());
        expression = expression.Replace("eventValue", baseValue.ToString());
        expression = expression.Replace("UnblockedDamage", baseValue.ToString());
        expression = expression.Replace("HasLostHpThisTurn", hasLostHpThisTurn.ToString());
        return expression;
    }

    private static string ReplaceFunctionCalls(string expression, PlayerData player)
    {
        expression = ReplaceBuffStackFunction(expression, "GetBuffStack", player);
        expression = ReplaceBuffStackFunction(expression, "Stack", player);
        return expression;
    }

    private static string ReplaceBuffStackFunction(string expression, string functionName, PlayerData player)
    {
        string pattern = $@"{functionName}\((\d+)\)";
        while (Regex.IsMatch(expression, pattern))
        {
            Match match = Regex.Match(expression, pattern);
            if (!match.Success)
            {
                break;
            }

            int buffId = int.Parse(match.Groups[1].Value);
            int stack = player != null ? player.GetBuffStack(buffId) : 0;
            expression = expression.Replace(match.Value, stack.ToString());
        }

        return expression;
    }

    private static string EvaluateMinFunctions(string expression)
    {
        string pattern = @"Min\(([^(),]+),([^(),]+)\)";
        while (Regex.IsMatch(expression, pattern))
        {
            Match match = Regex.Match(expression, pattern);
            if (!match.Success)
            {
                break;
            }

            int left = EvaluateSimpleExpression(match.Groups[1].Value);
            int right = EvaluateSimpleExpression(match.Groups[2].Value);
            int result = Mathf.Min(left, right);
            expression = expression.Replace(match.Value, result.ToString());
        }

        return expression;
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

        if (TryEvaluateFlatExpression(expression, out float result))
        {
            return Mathf.RoundToInt(result);
        }

        return 0;
    }

    private static bool TryEvaluateFlatExpression(string expression, out float result)
    {
        result = 0f;
        if (!TryTokenizeExpression(expression, out List<string> tokens))
        {
            return false;
        }

        if (!float.TryParse(tokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float currentValue))
        {
            return false;
        }

        List<string> reducedTokens = new List<string>
        {
            currentValue.ToString(CultureInfo.InvariantCulture)
        };

        for (int index = 1; index < tokens.Count; index += 2)
        {
            if (index + 1 >= tokens.Count)
            {
                return false;
            }

            string op = tokens[index];
            if (!float.TryParse(tokens[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float nextValue))
            {
                return false;
            }

            if (op == "*" || op == "/")
            {
                currentValue = op == "*"
                    ? currentValue * nextValue
                    : (Mathf.Approximately(nextValue, 0f) ? 0f : currentValue / nextValue);
                reducedTokens[reducedTokens.Count - 1] = currentValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (op == "+" || op == "-")
            {
                reducedTokens.Add(op);
                reducedTokens.Add(tokens[index + 1]);
                currentValue = nextValue;
            }
            else
            {
                return false;
            }
        }

        if (!float.TryParse(reducedTokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return false;
        }

        for (int index = 1; index < reducedTokens.Count; index += 2)
        {
            if (index + 1 >= reducedTokens.Count)
            {
                return false;
            }

            if (!float.TryParse(reducedTokens[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float nextValue))
            {
                return false;
            }

            result = reducedTokens[index] == "+"
                ? result + nextValue
                : result - nextValue;
        }

        return true;
    }

    private static bool TryTokenizeExpression(string expression, out List<string> tokens)
    {
        tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        int index = 0;
        while (index < expression.Length)
        {
            char current = expression[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (current == '+' || current == '-')
            {
                bool isUnary = tokens.Count == 0 || IsOperatorToken(tokens[tokens.Count - 1]);
                if (isUnary)
                {
                    if (!TryReadNumber(expression, ref index, out string signedNumber))
                    {
                        return false;
                    }

                    tokens.Add(signedNumber);
                    continue;
                }
            }

            if (current == '*' || current == '/' || current == '+' || current == '-')
            {
                tokens.Add(current.ToString());
                index++;
                continue;
            }

            if (char.IsDigit(current) || current == '.')
            {
                if (!TryReadNumber(expression, ref index, out string number))
                {
                    return false;
                }

                tokens.Add(number);
                continue;
            }

            return false;
        }

        return tokens.Count > 0;
    }

    private static bool TryReadNumber(string expression, ref int index, out string numberToken)
    {
        int start = index;
        if (expression[index] == '+' || expression[index] == '-')
        {
            index++;
        }

        bool hasDigit = false;
        bool hasDecimalPoint = false;

        while (index < expression.Length)
        {
            char current = expression[index];
            if (char.IsDigit(current))
            {
                hasDigit = true;
                index++;
                continue;
            }

            if (current == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
                index++;
                continue;
            }

            break;
        }

        if (!hasDigit)
        {
            numberToken = string.Empty;
            return false;
        }

        numberToken = expression.Substring(start, index - start);
        return true;
    }

    private static bool IsOperatorToken(string token)
    {
        return token == "+" || token == "-" || token == "*" || token == "/";
    }
}
