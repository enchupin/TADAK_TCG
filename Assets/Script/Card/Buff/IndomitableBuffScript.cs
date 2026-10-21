using static BattleRuntimeDefinitions;

public sealed class IndomitableBuffScript : PlayerBuffScript
{
    public override int BuffId => IndomitableBuffId;

    public override void OnPlayerHpLost(TrainingBattleManager battleManager, PlayerData player, int hpLoss, int stack)
    {
        if (battleManager == null || player == null || hpLoss <= 0 || stack <= 0 || !IsPlayerTurnState(battleManager))
        {
            return;
        }

        // v1.0 불굴은 힘 대신 보호막을 얻음
        player.AddDefense(stack);
    }

    private static bool IsPlayerTurnState(TrainingBattleManager battleManager)
    {
        return battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart
            || battleManager.CurrentTurnState == BattleTurnState.PlayerAction
            || battleManager.CurrentTurnState == BattleTurnState.PlayerTurnEnd;
    }
}
