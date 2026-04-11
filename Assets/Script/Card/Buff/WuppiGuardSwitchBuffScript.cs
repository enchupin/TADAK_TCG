using static BattleRuntimeDefinitions;

public sealed class WuppiGuardSwitchBuffScript : PlayerBuffScript
{
    public override int BuffId => WuppiGuardSwitchBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (stack <= 0)
        {
            return;
        }

        WuppiModeRuntimeUtility.ChangeMode(battleManager, player, WuppiModeState.Guard);
    }
}
