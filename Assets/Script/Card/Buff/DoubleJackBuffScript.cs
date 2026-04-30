using static BattleRuntimeDefinitions;

public sealed class DoubleJackBuffScript : PlayerBuffScript
{
    private int playedCardCounter;

    public override int BuffId => DoubleJackBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        playedCardCounter = 0;
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (player == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return;
        }

        playedCardCounter++;
        while (playedCardCounter >= 2)
        {
            playedCardCounter -= 2;
            player.Heal(1);
        }
    }
}
