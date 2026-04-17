using static BattleRuntimeDefinitions;

public sealed class LastStandBuffScript : PlayerBuffScript
{
    private bool isActiveUntilTurnEnd;

    public override int BuffId => LastStandBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        isActiveUntilTurnEnd = false;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        isActiveUntilTurnEnd = false;
    }

    public override bool TryConsumeFatalDamage(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || player.hp > 0 || !IsPlayerTurnState(battleManager))
        {
            return false;
        }

        if (isActiveUntilTurnEnd)
        {
            player.hp = 1;
            return true;
        }

        if (stack <= 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        isActiveUntilTurnEnd = true;
        player.hp = 1;
        return true;
    }

    private static bool IsPlayerTurnState(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return false;
        }

        return battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart
            || battleManager.CurrentTurnState == BattleTurnState.PlayerAction
            || battleManager.CurrentTurnState == BattleTurnState.PlayerTurnEnd;
    }
}
