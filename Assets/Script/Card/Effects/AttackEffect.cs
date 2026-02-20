using UnityEngine;

public class AttackEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = amount;
        
        // Use FormulaEvaluator with BattleContext and PlayerData
        if (!string.IsNullOrEmpty(amountFormula))
        {
            finalAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }

        // Apply damage based on target
        // Currently, TrainingBattleManager only has one monster, so we target it.
        // In the future, if multiple monsters exist, we'd iterate or use selection logic.
        Monster targetMonster = UnityEngine.Object.FindFirstObjectByType<Monster>();
        if (targetMonster != null)
        {
            // TODO: Trigger OnAttack events if needed (e.g. from battleManager.playerData)
            
            targetMonster.TakeDamage(finalAmount, battleManager.playerData.strength);
        }
    }
}
