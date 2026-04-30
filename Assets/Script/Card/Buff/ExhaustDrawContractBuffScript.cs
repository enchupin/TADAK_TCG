using static BattleRuntimeDefinitions;

public sealed class ExhaustDrawContractBuffScript : PlayerBuffScript
{
    public override int BuffId => ExhaustDrawContractBuffId;

    public override void OnCardsExhausted(TrainingBattleManager battleManager, PlayerData player, int stack, int exhaustedCount)
    {
        if (battleManager == null || player == null || stack <= 0 || exhaustedCount <= 0)
        {
            return;
        }

        battleManager.DrawCards(exhaustedCount * stack);
    }
}
