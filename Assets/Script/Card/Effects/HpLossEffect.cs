using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HpLossEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null)
        {
            return;
        }

        int resolvedAmount = ResolveAmount(battleManager, forwardedAmount);
        if (resolvedAmount <= 0)
        {
            return;
        }

        switch (target)
        {
            case TargetType.Self:
                int selfHpLoss = battleManager.playerData != null
                    ? battleManager.playerData.LoseHp(resolvedAmount)
                    : 0;
                battleManager.battleContext?.OnPlayerCardHpLost(selfHpLoss);
                break;
            case TargetType.RandomEnemy:
                Monster randomTarget = BuffCardUtility.PickRandomLivingMonster(battleManager);
                randomTarget?.LoseHp(resolvedAmount);
                break;
            default:
                List<Monster> targets = CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
                foreach (Monster monster in targets)
                {
                    monster?.LoseHp(resolvedAmount);
                }
                break;
        }

        battleManager.UpdateAllUI();
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager?.battleContext, battleManager?.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
