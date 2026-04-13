using UnityEngine;

/// <summary>
/// 스태미나 회복 이펙트
/// 현재 전투에서는 에너지 회복으로 처리
/// </summary>
[System.Serializable]
public class StaminaEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

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
        if (battleManager?.playerData == null)
        {
            return;
        }

        int staminaAmount = ResolveAmount(battleManager, forwardedAmount);
        if (staminaAmount == 0)
        {
            return;
        }

        if (staminaAmount > 0)
        {
            battleManager.playerData.AddEnergy(staminaAmount);
        }
        else
        {
            battleManager.playerData.LoseEnergy(-staminaAmount);
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount);
        }

        if (amount != 0)
        {
            return amount;
        }

        return forwardedAmount;
    }
}
