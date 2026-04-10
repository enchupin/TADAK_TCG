using static BattleRuntimeDefinitions;

public sealed class ColdAirBuffScript : PlayerBuffScript
{
    public override int BuffId => ColdAirBuffId;

    public override void OnPlayerBarrierReduced(TrainingBattleManager battleManager, PlayerData player, int reducedAmount, int stack)
    {
        if (battleManager == null || reducedAmount <= 0 || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToAllEnemies(FreezeBuffId, stack);
    }
}
