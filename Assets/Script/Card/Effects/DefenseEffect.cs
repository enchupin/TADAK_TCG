using UnityEngine;

/// <summary>
/// 방어 효과
/// 플레이어에게 방어력을 추가합니다.
/// </summary>
[System.Serializable]
public class DefenseEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        battleManager.playerData.AddDefense(finalAmount);
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
