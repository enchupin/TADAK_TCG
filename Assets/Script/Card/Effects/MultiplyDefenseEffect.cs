using UnityEngine;

/// <summary>
/// 방어력 배수 증가 효과
/// </summary>
public class MultiplyDefenseEffect : ICardEffect
{
    public int amount;  // 배수 (2 = 2배)
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int multiplier = GetAmount(battleManager.battleContext, battleManager.playerData);
        int currentDefense = battleManager.playerData.defense;
        int additionalDefense = currentDefense * (multiplier - 1);
        
        if (additionalDefense > 0)
        {
            battleManager.playerData.AddDefense(additionalDefense);
            Debug.Log($"[MultiplyDefenseEffect] 방어력 {multiplier}배 증가: {currentDefense} → {battleManager.playerData.defense}");
        }
        
        battleManager.UpdateAllUI();
    }
    
    public int GetAmount(BattleContext context, PlayerData player = null)
    {
        if (!string.IsNullOrEmpty(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, context, player);
        }
        return amount;
    }
}
