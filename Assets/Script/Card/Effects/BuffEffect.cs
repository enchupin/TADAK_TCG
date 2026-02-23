using UnityEngine;

/// <summary>
/// 버프 효과
/// 플레이어에게 버프를 부여합니다.
/// </summary>
[System.Serializable]
public class BuffEffect : ICardEffect
{
    public string stat; // Deprecated but kept for legacy
    public int buffId;
    public int amount;
    public string amountFormula;
    public int duration;
    public TargetType target;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        
        if (buffId > 0)
        {
            // Apply by BuffID using BuffManager
            ApplyBuff(battleManager, finalAmount);
        }
        else
        {
            // Fallback to legacy string-based stat
            ApplyLegacyBuff(battleManager, finalAmount);
        }
        
        battleManager.UpdateAllUI();
    }
    
    private void ApplyBuff(TrainingBattleManager manager, int finalAmount)
    {
        if (target == TargetType.Self)
        {
            manager.playerData.AddBuff(buffId, finalAmount);
        }
        else if (target == TargetType.SingleEnemy || target == TargetType.AllEnemies)
        {
            Monster[] monsters = UnityEngine.Object.FindObjectsByType<Monster>(UnityEngine.FindObjectsSortMode.None);
            foreach (var m in monsters)
            {
                m.AddBuff(buffId, finalAmount);
            }
        }
    }
    
    private void ApplyLegacyBuff(TrainingBattleManager manager, int finalAmount)
    {
        switch (stat?.ToLower())
        {
            case "strength":
            case "힘":
                manager.playerData.AddStrength(finalAmount);
                break;
            default:
                Debug.LogWarning($"[BuffEffect] Unknown stat: {stat}");
                break;
        }
    }
    
}
