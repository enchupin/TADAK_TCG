using static BattleRuntimeDefinitions;

public sealed class LastStandBuffScript : PlayerBuffScript
{
    private bool isActiveUntilTurnEnd;
    private int activationTurnSequence;

    public override int BuffId => LastStandBuffId;

    public override int GetRuntimeStack(TrainingBattleManager battleManager, PlayerData player)
    {
        if (!IsProtectionWindow(battleManager))
        {
            isActiveUntilTurnEnd = false;
            return 0;
        }
        if (activationTurnSequence != battleManager.TurnSequence)
            isActiveUntilTurnEnd = false;
        return isActiveUntilTurnEnd ? 1 : base.GetRuntimeStack(battleManager, player);
    }

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        isActiveUntilTurnEnd = false;
    }

    public override bool TryConsumeFatalDamage(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || player.hp > 0 || GetRuntimeStack(battleManager, player) <= 0)
        {
            return false;
        }

        if (!isActiveUntilTurnEnd)
        {
            player.RemoveBuffStack(BuffId);
            isActiveUntilTurnEnd = true;
            activationTurnSequence = battleManager.TurnSequence;
        }
        player.hp = 1;
        return true;
    }

    private static bool IsProtectionWindow(TrainingBattleManager battleManager)
    {
        return battleManager != null
            && (battleManager.CurrentTurnState == BattleTurnState.PlayerTurnStart
                || battleManager.CurrentTurnState == BattleTurnState.PlayerAction);
    }
}
