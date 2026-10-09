using static BattleRuntimeDefinitions;

public sealed class PotionCycleBuffScript : PlayerBuffScript
{
    public override int BuffId => PotionCycleBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || playedCard == null || isRepeatedEffect || stack <= 0 || !BuffCardUtility.IsPotionCard(playedCard))
        {
            return;
        }

        battleManager.DrawCards(stack);
    }
}
