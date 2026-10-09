using static BattleRuntimeDefinitions;

public sealed class RegenerationBuffScript : PlayerBuffScript
{
    public override int BuffId => RegenerationBuffId;

    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player != null && player.hp > 0 && stack > 0) player.Heal(stack);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
