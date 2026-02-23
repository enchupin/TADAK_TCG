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
        int healAmount = EffectAmountResolver.Resolve(amount, amountFormula, battleManager.battleContext, battleManager.playerData);
        
        if (target == TargetType.Self)
        {
            battleManager.playerData.Heal(healAmount);
            Debug.Log($"[HealEffect] 플레이어 체력 +{healAmount} 회복");
        }
        
        battleManager.UpdateAllUI();
    }
    
}
