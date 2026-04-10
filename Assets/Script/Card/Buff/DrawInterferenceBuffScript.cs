using static BattleRuntimeDefinitions;

public sealed class DrawInterferenceBuffScript : PlayerBuffScript
{
    public override int BuffId => DrawInterferenceBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || player == null || stack <= 0)
        {
            return;
        }

        battleManager.AddTurnStartDrawModifier(-stack);
        player.ConsumeBuffStack(BuffId, stack);
    }
}
