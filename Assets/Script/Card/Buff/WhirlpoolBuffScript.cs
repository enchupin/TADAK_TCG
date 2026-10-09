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
