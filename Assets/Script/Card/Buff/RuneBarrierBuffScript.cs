using static BattleRuntimeDefinitions;

public sealed class RuneBarrierBuffScript : PlayerBuffScript
{
    public override int BuffId => RuneBarrierBuffId;

    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        int runeStack = player.GetBuffStack(RuneBuffId);
        if (runeStack > 0)
        {
            player.AddDefense(runeStack * stack);
        }
    }
}
