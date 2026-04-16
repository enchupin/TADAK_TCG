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

        battleManager.ApplyBuffToPlayer(StrengthBuffId, stack);
    }

    private static bool IsPlayerTurnState(TrainingBattleManager battleManager)
    {
        return battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart
            || battleManager.CurrentTurnState == BattleTurnState.PlayerAction
            || battleManager.CurrentTurnState == BattleTurnState.PlayerTurnEnd;
    }
}
