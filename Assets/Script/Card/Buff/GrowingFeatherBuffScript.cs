using static BattleRuntimeDefinitions;

public sealed class GrowingFeatherBuffScript : PlayerBuffScript
{
    public override int BuffId => GrowingFeatherBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(FeatherBuffId, stack);
    }
}
