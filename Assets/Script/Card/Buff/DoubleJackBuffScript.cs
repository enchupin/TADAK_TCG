using static BattleRuntimeDefinitions;

// 1007 더블: 같은 비용의 카드를 연속 2장 쓸 때마다 카드를 1장 뽑음
public sealed class DoubleJackBuffScript : PlayerBuffScript
{
    private const int RequiredStreak = 2;
    private const int DrawAmount = 1;

    private int lastPaidCost = -1;
    private int sameCostStreak;

    public override int BuffId => DoubleJackBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        lastPaidCost = -1;
        sameCostStreak = 0;
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || playedCard == null || isRepeatedEffect || stack <= 0)
        {
            return;
        }

        int paidCost = battleManager.battleContext != null ? battleManager.battleContext.GetPaidCost(playedCard) : playedCard.cost;
        sameCostStreak = paidCost == lastPaidCost ? sameCostStreak + 1 : 1;
        lastPaidCost = paidCost;
        if (sameCostStreak < RequiredStreak)
        {
            return;
        }

        sameCostStreak = 0;
        lastPaidCost = -1;
        battleManager.DrawCards(DrawAmount);
    }
}
