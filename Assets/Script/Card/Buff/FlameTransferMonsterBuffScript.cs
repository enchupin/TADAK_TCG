using static BattleRuntimeDefinitions;

public sealed class FlameTransferMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => FlameTransferBuffId;

    public override void OnMonsterDeath(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (battleManager == null || monster == null || stack <= 0)
        {
            return;
        }

        int burnStack = monster.GetBuffStack(BurnBuffId);
        if (burnStack > 0)
        {
            battleManager.ApplyBuffToAllEnemies(BurnBuffId, burnStack);
        }
    }
}
