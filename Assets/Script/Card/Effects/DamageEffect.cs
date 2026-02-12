using UnityEngine;

/// <summary>
/// 데미지 효과
/// 적에게 데미지를 입힙니다.
/// </summary>
[System.Serializable]
public class DamageEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.SingleEnemy;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        
        switch (target)
        {
            case TargetType.SingleEnemy:
                if (battleManager.monster != null)
                {
                    int damageDealt = battleManager.monster.TakeDamage(finalAmount, battleManager.playerData.strength);
                    battleManager.battleContext.OnDamageDealt(damageDealt);
                }
                break;
            case TargetType.AllEnemies:
                // 여러 적 지원 시 구현
                if (battleManager.monster != null)
                {
                    int damageDealt = battleManager.monster.TakeDamage(finalAmount, battleManager.playerData.strength);
                    battleManager.battleContext.OnDamageDealt(damageDealt);
                }
                break;
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

/// <summary>
/// 타겟 타입 열거형
/// </summary>
public enum TargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    AllAllies,
    Hand,
    Discard,
    Deck,
    None
}
