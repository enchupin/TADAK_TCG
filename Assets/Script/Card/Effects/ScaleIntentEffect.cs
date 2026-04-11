using System.Collections.Generic;

[System.Serializable]
public class ScaleIntentEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;
    public float multiplier = 1f;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        float resolvedMultiplier = multiplier > 0f ? multiplier : 1f;
        foreach (Monster monster in ResolveTargets(battleManager))
        {
            monster?.ScalePlannedIntent(resolvedMultiplier);
        }

        battleManager.UpdateAllUI();
    }

    private List<Monster> ResolveTargets(TrainingBattleManager battleManager)
    {
        if (target == TargetType.RandomEnemy)
        {
            Monster randomTarget = BuffCardUtility.PickRandomLivingMonster(battleManager);
            return randomTarget != null ? new List<Monster> { randomTarget } : new List<Monster>();
        }

        return CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target);
    }
}
