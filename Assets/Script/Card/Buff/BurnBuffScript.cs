using static BattleRuntimeDefinitions;

public sealed class BurnBuffScript : PlayerBuffScript
{
    public override int BuffId => BurnBuffId;

    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.TakeDamage(stack);
    }
}
