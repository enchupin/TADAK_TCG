using System;
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
    public static int Evaluate(string formula, BattleContext context, PlayerData player = null)
    {
        if (string.IsNullOrEmpty(formula))
        {
            return 0;
        }
        
        // 숫자면 바로 반환
        if (int.TryParse(formula, out int result))
        {
            return result;
        }
        
        // 공백 제거
        formula = formula.Replace(" ", "");
        
        // 특수 키워드 처리
        switch (formula.ToLower())
        {
            case "all":
                // "all"은 컨텍스트에 따라 다름 (방어도 전체 등)
                return player != null ? player.defense : 0;
                
            case "usecardincombat":
                return context.cardsPlayedThisCombat;
                
            case "consumed":
                return context.defenseConsumed;
                
            case "discarded":
                return context.cardsDiscardedThisTurn;
                
            case "exhausted":
                return context.cardsExhaustedThisTurn;
                
            case "finaldamage":
                return context.lastDamageDealt;
                
            case "value":
                // 버프 스택 값 (별도 처리 필요)
                return 0;
        }
        
        // 수식 평가 (간단한 사칙연산)
        try
        {
            // 변수 치환
            string expression = formula;
            expression = expression.Replace("UseCardInCombat", context.cardsPlayedThisCombat.ToString());
            expression = expression.Replace("consumed", context.defenseConsumed.ToString());
            expression = expression.Replace("discarded", context.cardsDiscardedThisTurn.ToString());
            expression = expression.Replace("exhausted", context.cardsExhaustedThisTurn.ToString());
            expression = expression.Replace("finalDamage", context.lastDamageDealt.ToString());
            
            // 간단한 수식 계산 (*, /, +, -)
            return EvaluateSimpleExpression(expression);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FormulaEvaluator] Failed to evaluate formula: {formula}, Error: {e.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// 간단한 수식 계산 (사칙연산만 지원)
    /// </summary>
    private static int EvaluateSimpleExpression(string expression)
    {
        // 괄호 처리
        while (expression.Contains("("))
        {
            int start = expression.LastIndexOf('(');
            int end = expression.IndexOf(')', start);
            if (end == -1) break;
            
            string subExpr = expression.Substring(start + 1, end - start - 1);
            int subResult = EvaluateSimpleExpression(subExpr);
            expression = expression.Substring(0, start) + subResult + expression.Substring(end + 1);
        }
        
        // 곱셈, 나눗셈 먼저
        expression = EvaluateOperator(expression, '*');
        expression = EvaluateOperator(expression, '/');
        
        // 덧셈, 뺄셈
        expression = EvaluateOperator(expression, '+');
        expression = EvaluateOperator(expression, '-');
        
        // 최종 결과
        if (int.TryParse(expression, out int result))
        {
            return result;
        }
        
        // 실수 처리 (0.25 * damageValue 같은 경우)
        if (float.TryParse(expression, out float floatResult))
        {
            return Mathf.RoundToInt(floatResult);
        }
        
        return 0;
    }
    
    /// <summary>
    /// 특정 연산자 처리
    /// </summary>
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
