using static BattleRuntimeDefinitions;

public sealed class MirrorMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => MirrorBuffId;

    public override void OnMonsterHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
        if (monster == null || hpLoss <= 0)
        {
            return;
        }

        monster.AddBuff(BuffId, hpLoss);
    }

    public override int ModifyOutgoingDamage(TrainingBattleManager battleManager, Monster monster, int stack, int currentDamage)
    {
        return currentDamage + stack;
    }

    public override void OnMonsterAttackResolved(TrainingBattleManager battleManager, Monster monster, PlayerData target, int attemptedDamage, int hpDamage, int stack)
    {
        if (monster == null || attemptedDamage <= 0 || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, stack);
    }
}
