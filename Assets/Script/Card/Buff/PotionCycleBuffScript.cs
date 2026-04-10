using static BattleRuntimeDefinitions;

public sealed class PotionCycleBuffScript : PlayerBuffScript
{
    public override int BuffId => PotionCycleBuffId;

    public override bool ShouldPotionGoToDiscardInsteadOfExhaust(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, bool currentShouldDiscard)
    {
        return currentShouldDiscard || (stack > 0 && BuffCardUtility.IsPotionCard(card));
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || playedCard == null || isRepeatedEffect || stack <= 0 || !BuffCardUtility.IsPotionCard(playedCard))
        {
            return;
        }

        battleManager.DrawCards(stack);
    }
}
