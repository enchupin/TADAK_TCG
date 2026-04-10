using static BattleRuntimeDefinitions;

public sealed class BarrierRetentionBuffScript : PlayerBuffScript
{
    public override int BuffId => BarrierRetentionBuffId;

    public override bool TryConsumeBarrierRetentionOnTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        return true;
    }
}
