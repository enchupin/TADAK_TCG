using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class PranksterGhostBuffScript : MonsterBuffScript
{
    public override int BuffId => PranksterGhostBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
}
