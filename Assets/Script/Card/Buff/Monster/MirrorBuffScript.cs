using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class MirrorBuffScript : MonsterBuffScript
{
    public override int BuffId => MirrorBuffId;

    public override bool IsActive(Monster monster, int stack) => monster is MirrorMonster || stack > 0;

    public override void OnMonsterHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
        if (monster == null || hpLoss <= 0)
        {
            return;
        }

        monster.AddBuff(BuffId, hpLoss);
    }

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        return currentBonus + stack;
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
}
