using static BattleRuntimeDefinitions;

public sealed class RegenerationMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => RegenerationBuffId;

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.Heal(stack);
        monster.ConsumeBuffStack(BuffId, 1);
    }
}
