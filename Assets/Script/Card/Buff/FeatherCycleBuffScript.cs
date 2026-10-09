using static BattleRuntimeDefinitions;

public sealed class FeatherCycleBuffScript : PlayerBuffScript
{
    public override int BuffId => FeatherCycleBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || stack <= 0 || isRepeatedEffect || !BuffCardUtility.IsFeatherCard(playedCard))
        {
            return;
        }

        battleManager.DrawCards(stack);
    }
}
