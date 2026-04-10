using static BattleRuntimeDefinitions;

public sealed class LifeLinkMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => LifeLinkBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }

    public override void OnPlayerHpLost(TrainingBattleManager battleManager, Monster monster, int hpLoss, int stack)
    {
        if (monster == null || hpLoss <= 0 || stack <= 0)
        {
            return;
        }

        monster.Heal(hpLoss);
    }
}
