using static BattleRuntimeDefinitions;

public sealed class StrengthDecayBuffScript : PlayerBuffScript
{
    public override int BuffId => StrengthDecayBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(StrengthBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
