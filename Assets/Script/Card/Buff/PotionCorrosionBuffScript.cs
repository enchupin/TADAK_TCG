using static BattleRuntimeDefinitions;

public sealed class PotionCorrosionBuffScript : PlayerBuffScript
{
    public override int BuffId => CorrosionOnPotionUseBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || playedCard == null || isRepeatedEffect || stack <= 0 || !BuffCardUtility.IsPotionCard(playedCard))
        {
            return;
        }

        Monster randomTarget = BuffCardUtility.PickRandomLivingMonster(battleManager);
        if (randomTarget != null)
        {
            battleManager.ApplyBuffToMonster(randomTarget, CorrosionBuffId, stack);
        }
    }
}
