using static BattleRuntimeDefinitions;

public sealed class DoubleFeatherBuffScript : PlayerBuffScript
{
    public override int BuffId => DoubleFeatherBuffId;

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return 0;
        }

        if (!BuffCardUtility.IsFeatherCard(playedCard))
        {
            return 0;
        }

        return stack;
    }
}
