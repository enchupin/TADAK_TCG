using UnityEngine;

/// <summary>
/// 드로우 효과
/// 카드를 뽑습니다.
/// </summary>
[System.Serializable]
public class DrawEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        battleManager.DrawCards(finalAmount);
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
