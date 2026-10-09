using static BattleRuntimeDefinitions;

public sealed class FeatherCycleBuffScript : PlayerBuffScript
{
    public override int BuffId => FeatherCycleBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || stack <= 0 || isRepeatedEffect || playedCard == null
            || (playedCard.cardId != 203080 && playedCard.cardId != 203081))
        {
            return;
        }

        battleManager.DrawCards(stack);
    }
}
