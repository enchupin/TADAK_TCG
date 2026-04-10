using static BattleRuntimeDefinitions;

public sealed class OverheatDecayBuffScript : PlayerBuffScript
{
    public override int BuffId => OverheatDecayBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(OverheatBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
