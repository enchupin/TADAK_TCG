using static BattleRuntimeDefinitions;

public sealed class RepeatNextPowerCardBuffScript : PlayerBuffScript
{
    public override int BuffId => RepeatNextPowerCardBuffId;

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return 0;
        }

        if (!playedCard.HasKeyword(CardKeywordIds.Power))
        {
            return 0;
        }

        player.ConsumeBuffStack(BuffId, stack);
        return stack;
    }
}
