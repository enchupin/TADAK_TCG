using System.Collections.Generic;

[System.Serializable]
public class MultiplyEnemyDebuffsEffect : ICardEffect
{
    public int amount;
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int multiplier = amount > 1 ? amount : 2;
        foreach (Monster monster in ResolveTargets(battleManager))
        {
            if (monster == null || monster.IsDead())
            {
                continue;
            }

            foreach (Buff buff in monster.currentBuffs)
            {
                if (buff?.data == null || buff.stack <= 0 || BuffData.IsBeneficialBuffId(buff.data.buffId))
                {
                    continue;
                }

                buff.stack *= multiplier;
            }

            monster.UpdateUI();
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
