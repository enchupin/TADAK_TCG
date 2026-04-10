using static BattleRuntimeDefinitions;

public sealed class CounterattackDecayBuffScript : PlayerBuffScript
{
    public override int BuffId => CounterattackDecayBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(CounterattackBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
