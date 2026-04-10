using static BattleRuntimeDefinitions;

public sealed class UnplayableUnlockBuffScript : PlayerBuffScript
{
    public override int BuffId => UnplayableUnlockBuffId;

    public override bool CanPlayCard(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentCanPlay)
    {
        if (currentCanPlay || card == null || stack <= 0)
        {
            return currentCanPlay;
        }

        return card.HasKeyword(CardKeywordIds.Unplayable);
    }

    public override bool ShouldExhaustUnlockedUnplayableCard(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentShouldExhaust)
    {
        if (currentShouldExhaust || card == null || stack <= 0)
        {
            return currentShouldExhaust;
        }

        return card.HasKeyword(CardKeywordIds.Unplayable);
    }
}
