using static BattleRuntimeDefinitions;

public sealed class NextCardFreeBuffScript : PlayerBuffScript
{
    public override int BuffId => NextCardFreeBuffId;

    public override int GetEffectiveCardCost(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, int currentCost)
    {
        return stack > 0 ? 0 : currentCost;
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (player == null || stack <= 0 || isRepeatedEffect)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
