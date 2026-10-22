using static BattleRuntimeDefinitions;

public sealed class HighCostRepeatBuffScript : PlayerBuffScript
{
    private const int MinimumCost = 3;

    private bool hasRepeatedThisTurn;

    public override int BuffId => HighCostRepeatBuffId;

    // 툴팁 진행도: 이번 턴 발동 여부
    public bool HasRepeatedThisTurn => hasRepeatedThisTurn;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        hasRepeatedThisTurn = false;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        hasRepeatedThisTurn = false;
    }

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        // 할인 후 실제로 지불하는 비용 기준
        if (playedCard == null || isRepeatedEffect || stack <= 0 || hasRepeatedThisTurn
            || battleManager?.battleContext == null || battleManager.battleContext.GetPaidCost(playedCard) < MinimumCost)
        {
            return 0;
        }

        hasRepeatedThisTurn = true;
        return 1;
    }
}
