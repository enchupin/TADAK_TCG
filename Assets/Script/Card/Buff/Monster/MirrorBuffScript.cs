using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class MirrorBuffScript : MonsterBuffScript
{
    public override int BuffId => MirrorBuffId;

    public override bool IsActive(Monster monster, int stack) => monster is MirrorMonster || stack > 0;

    // 보호막에 막힌 피해를 포함해 받은 피해 전량만큼 중첩 (E-15)
    public override void OnMonsterAfterTakeDamage(TrainingBattleManager battleManager, Monster monster, int incomingDamage, int damageAfterDefense, int stack)
    {
        if (monster == null || incomingDamage <= 0)
        {
            return;
        }

        monster.AddBuff(BuffId, incomingDamage);
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
