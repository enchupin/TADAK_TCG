using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class LifeLinkBuffScript : MonsterBuffScript
{
    public override int BuffId => LifeLinkBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, stack);
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster != null && stack > 0) monster.ConsumeBuffStack(BuffId, stack);
    }

    public override void OnMonsterHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
        if (monster == null || hpLoss <= 0 || stack <= 0)
        {
            return;
        }

        battleManager?.playerData?.Heal(hpLoss);
    }
}
}
