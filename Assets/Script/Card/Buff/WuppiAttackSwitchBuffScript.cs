using static BattleRuntimeDefinitions;

public sealed class WuppiAttackSwitchBuffScript : PlayerBuffScript
{
    public override int BuffId => WuppiAttackSwitchBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (stack <= 0)
        {
            return;
        }

        WuppiModeRuntimeUtility.ChangeMode(battleManager, player, WuppiModeState.Attack);
    }
}
