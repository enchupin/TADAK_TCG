using UnityEngine;

[System.Serializable]
public sealed class ApplyBuffByTargetStackEffect : ICardEffect
{
    public int buffId;
    public int amount;
    public int count;
    public int baseAmount;
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null || buffId <= 0)
        {
            return;
        }

        int unitSize = count > 0 ? count : 10;
        int unitAmount = amount > 0 ? amount : 1;
        int fixedAmount = Mathf.Max(0, baseAmount);

        foreach (Monster monster in CardEffectRuntimeUtility.ResolveEnemyTargets(battleManager, target))
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            int sourceStack = Mathf.Max(0, monster.GetBuffStack(buffId));
            int finalAmount = fixedAmount + (sourceStack / unitSize) * unitAmount;
            if (finalAmount <= 0)
            {
                continue;
            }

            battleManager.ApplyBuffToMonster(monster, buffId, finalAmount);
        }

        battleManager.UpdateAllUI();
    }
}
