using static BattleRuntimeDefinitions;

public sealed class DeadlyAmbushBuffScript : PlayerBuffScript
{
    private bool hasConsumedThisTurn;

    public override int BuffId => DeadlyAmbushBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        hasConsumedThisTurn = false;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        hasConsumedThisTurn = false;
    }

    public override int GetFeatherTriggerBonus(TrainingBattleManager battleManager, PlayerData player, int stack, int currentBonus)
    {
        if (hasConsumedThisTurn || stack <= 0)
        {
            return currentBonus;
        }

        hasConsumedThisTurn = true;
        return currentBonus + stack;
    }
}
