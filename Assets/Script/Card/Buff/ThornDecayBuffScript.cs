using static BattleRuntimeDefinitions;

public sealed class ThornDecayBuffScript : PlayerBuffScript
{
    public override int BuffId => ThornDecayBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(ThornBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
