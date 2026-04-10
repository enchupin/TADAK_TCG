using static BattleRuntimeDefinitions;

public sealed class PermanentBarrierRetentionBuffScript : PlayerBuffScript
{
    public override int BuffId => PermanentBarrierRetentionBuffId;

    public override bool HasPermanentBarrierRetention(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentHasRetention)
    {
        return currentHasRetention || stack > 0;
    }
}
