using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체력 회복 효과
/// </summary>
public class HealEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<int> cardIdList;
    public TargetType target;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int healAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList);
        
        if (target == TargetType.Self)
        {
            battleManager.playerData.Heal(healAmount);
            Debug.Log($"[HealEffect] 플레이어 체력 +{healAmount} 회복");
        }
        
        battleManager.UpdateAllUI();
    }
    
}
