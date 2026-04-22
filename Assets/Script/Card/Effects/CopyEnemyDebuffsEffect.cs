using System.Collections.Generic;

[System.Serializable]
public sealed class CopyEnemyDebuffsEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        Monster sourceMonster = ResolveSourceMonster(battleManager);
        if (sourceMonster == null || sourceMonster.currentBuffs == null)
        {
            return;
        }

        List<BuffSnapshot> debuffs = new List<BuffSnapshot>();
        foreach (Buff buff in sourceMonster.currentBuffs)
        {
            if (buff?.data == null || buff.stack <= 0 || BuffData.IsBeneficialBuffId(buff.data.buffId))
            {
                continue;
            }

            debuffs.Add(new BuffSnapshot(buff.data.buffId, buff.stack));
        }

        if (debuffs.Count == 0)
        {
            return;
        }

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            if (monster == null || monster == sourceMonster || monster.IsDead())
            {
                continue;
            }

            foreach (BuffSnapshot debuff in debuffs)
            {
                battleManager.ApplyBuffToMonster(monster, debuff.BuffId, debuff.Stack);
            }
        }

        battleManager.UpdateAllUI();
    }

    private Monster ResolveSourceMonster(TrainingBattleManager battleManager)
    {
        if (target == TargetType.RandomEnemy)
        {
            return BuffCardUtility.PickRandomLivingMonster(battleManager);
        }

        return CardEffectRuntimeUtility.ResolveSingleEnemyTarget(battleManager);
    }

    private readonly struct BuffSnapshot
    {
        public readonly int BuffId;
        public readonly int Stack;

        public BuffSnapshot(int buffId, int stack)
        {
            BuffId = buffId;
            Stack = stack;
        }
    }
}
