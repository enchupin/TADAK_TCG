using UnityEngine;

/// <summary>
/// 체력 회복 효과
/// </summary>
public class HealEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int healAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        
        if (target == TargetType.Self)
        {
            battleManager.playerData.Heal(healAmount);
            Debug.Log($"[HealEffect] 플레이어 체력 +{healAmount} 회복");
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
