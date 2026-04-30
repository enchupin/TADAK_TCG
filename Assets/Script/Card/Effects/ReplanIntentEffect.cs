using System.Collections.Generic;
using UnityEngine;

public class ReplanIntentEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        List<Monster> targets = ResolveTargets(battleManager);
        foreach (Monster monster in targets)
        {
            monster?.PlanNextAction();
        }

        battleManager.UpdateAllUI();
    }

    private List<Monster> ResolveTargets(TrainingBattleManager battleManager)
    {
        List<Monster> targets = new List<Monster>();
        if (battleManager == null)
        {
            return targets;
        }

        switch (target)
        {
            case TargetType.AllEnemies:
                return battleManager.GetLivingMonsters() ?? new List<Monster>();

            case TargetType.RandomEnemy:
                Monster randomTarget = BuffCardUtility.PickRandomLivingMonster(battleManager);
                if (randomTarget != null)
                {
                    targets.Add(randomTarget);
                }
                return targets;

            case TargetType.None:
            case TargetType.SingleEnemy:
            default:
                Monster singleTarget = CardEffectRuntimeUtility.ResolveSingleEnemyTarget(battleManager);
                if (singleTarget != null)
                {
                    targets.Add(singleTarget);
                }
                return targets;
        }
    }
}
