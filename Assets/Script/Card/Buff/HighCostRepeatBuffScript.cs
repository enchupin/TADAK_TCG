using static BattleRuntimeDefinitions;

public sealed class HighCostRepeatBuffScript : PlayerBuffScript
{
    private int remainingRepeatCount;

    public override int BuffId => HighCostRepeatBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        remainingRepeatCount = 0;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        remainingRepeatCount = stack;
    }

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (playedCard == null || isRepeatedEffect || remainingRepeatCount <= 0 || playedCard.cost < 2)
        {
            return 0;
        }

        remainingRepeatCount--;
        return 1;
    }
}
