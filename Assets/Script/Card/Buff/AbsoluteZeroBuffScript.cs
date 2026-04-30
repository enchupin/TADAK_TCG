using static BattleRuntimeDefinitions;

public sealed class AbsoluteZeroBuffScript : PlayerBuffScript
{
    public override int BuffId => AbsoluteZeroBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToAllEnemies(FreezeBuffId, stack);
    }
}
