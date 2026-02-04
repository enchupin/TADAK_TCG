using UnityEngine;

/// <summary>
/// 버프 효과
/// 플레이어에게 버프를 부여합니다.
/// </summary>
[System.Serializable]
public class BuffEffect : ICardEffect
{
    public string stat;
    public int amount;
    public string amountFormula;
    public int duration;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = GetAmount(battleManager.battleContext, battleManager.playerData);
        
        // stat에 따라 버프 적용
        switch (stat?.ToLower())
        {
            case "strength":
            case "힘":
                battleManager.playerData.AddStrength(finalAmount);
                break;
            default:
                Debug.LogWarning($"[BuffEffect] Unknown stat: {stat}");
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
