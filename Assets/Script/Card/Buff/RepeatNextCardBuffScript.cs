using static BattleRuntimeDefinitions;

public sealed class RepeatNextCardBuffScript : PlayerBuffScript
{
    public override int BuffId => RepeatNextCardBuffId;

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return 0;
        }

        player.ConsumeBuffStack(BuffId, stack);
        return stack;
    }
}
