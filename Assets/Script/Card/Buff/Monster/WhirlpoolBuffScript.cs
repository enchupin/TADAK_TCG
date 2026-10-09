using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class WhirlpoolBuffScript : MonsterBuffScript
{
    public override int BuffId => WhirlpoolBuffId;

    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0 || monster.IsDead())
        {
            return;
        }

        monster.AddBuff(DrowningBuffId, stack);
    }
}
}
