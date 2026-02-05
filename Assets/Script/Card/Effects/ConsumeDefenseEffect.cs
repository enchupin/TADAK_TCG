using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 방어력 소모 효과 (중첩 효과 실행)
/// </summary>
public class ConsumeDefenseEffect : ICardEffect
{
    public int amount;  // 소모할 방어력 ("all"은 amountFormula로 처리)
    public string amountFormula;  // "all" 또는 수식
    public ICardEffect nestedEffect;  // 소모 후 실행할 효과
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int consumeAmount = GetConsumeAmount(battleManager);
        int actualConsumed = Mathf.Min(consumeAmount, battleManager.playerData.defense);
        
        // 방어력 소모
        battleManager.playerData.defense -= actualConsumed;
        Debug.Log($"[ConsumeDefenseEffect] 방어력 {actualConsumed} 소모 (남은 방어력: {battleManager.playerData.defense})");
        
        // 중첩 효과 실행 (consumed 값 전달)
        if (nestedEffect != null && actualConsumed > 0)
        {
            // BattleContext에 consumed 값 임시 저장
            int previousConsumed = battleManager.battleContext.defenseConsumed;
            battleManager.battleContext.defenseConsumed = actualConsumed;
            
            nestedEffect.Execute(battleManager);
            
            // 복원
            battleManager.battleContext.defenseConsumed = previousConsumed;
        }
        
        battleManager.UpdateAllUI();
    }
    
    private int GetConsumeAmount(TrainingBattleManager battleManager)
    {
        if (!string.IsNullOrEmpty(amountFormula))
        {
            // "all"인 경우
            if (amountFormula.ToLower() == "all")
            {
                return battleManager.playerData.defense;
            }
            
            // 수식 계산
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }
        
        return amount;
    }
}
