using static BattleRuntimeDefinitions;

public sealed class RegenerationBuffScript : PlayerBuffScript
{
    public override int BuffId => RegenerationBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.Heal(stack);
        player.ConsumeBuffStack(BuffId, 1);
    }
}
