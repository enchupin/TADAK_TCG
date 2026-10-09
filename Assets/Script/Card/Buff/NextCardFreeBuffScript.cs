using static BattleRuntimeDefinitions;

public sealed class NextCardFreeBuffScript : PlayerBuffScript
{
    public override int BuffId => NextCardFreeBuffId;

    public override int GetEffectiveCardCost(TrainingBattleManager battleManager, PlayerData player, Card card, int stack, int currentCost)
    {
        return stack > 0 ? 0 : currentCost;
    }

}
