using static BattleRuntimeDefinitions;

public sealed class WhirlpoolBuffScript : PlayerBuffScript
{
    public override int BuffId => WhirlpoolBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || player == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(DrowningBuffId, stack);
    }
}

public sealed class WhirlpoolMonsterBuffScript : MonsterBuffScript
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
