using static BattleRuntimeDefinitions;

public sealed class HighCostRepeatBuffScript : PlayerBuffScript
{
    private bool hasRepeatedThisTurn;

    public override int BuffId => HighCostRepeatBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        hasRepeatedThisTurn = false;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        hasRepeatedThisTurn = false;
    }

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (playedCard == null || isRepeatedEffect || stack <= 0 || hasRepeatedThisTurn || playedCard.cost < 2)
        {
            return 0;
        }

        hasRepeatedThisTurn = true;
        return 1;
    }
}
