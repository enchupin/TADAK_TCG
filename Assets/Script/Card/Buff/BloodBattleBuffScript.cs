using static BattleRuntimeDefinitions;

public sealed class BloodBattleBuffScript : PlayerBuffScript
{
    public override int BuffId => BloodBattleBuffId;

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return 0;
        }

        if (!BuffCardUtility.CausesSelfHpLoss(playedCard))
        {
            return 0;
        }

        return stack;
    }
}
