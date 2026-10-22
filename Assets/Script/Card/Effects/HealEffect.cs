using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체력 회복 이펙트
/// </summary>
public class HealEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<int> cardIdList;
    public TargetType target;
    // OverflowToBarrier: 최대 체력을 넘는 회복량을 보호막으로 전환
    public string operation;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        ExecuteInternal(battleManager, forwardedAmount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        int healAmount = ResolveHealAmount(battleManager, forwardedAmount);

        if (target == TargetType.Self)
        {
            int overflowAmount = Mathf.Max(0, healAmount - (battleManager.playerData.maxHP - battleManager.playerData.hp));
            battleManager.playerData.Heal(healAmount);
            if (overflowAmount > 0 && string.Equals(operation, "OverflowToBarrier", System.StringComparison.OrdinalIgnoreCase))
            {
                battleManager.playerData.AddDefense(overflowAmount, true);
            }
            Debug.Log($"[HealEffect] 플레이어 체력 +{healAmount} 회복");
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveHealAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList, forwardedAmount > 0 ? forwardedAmount : amount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
