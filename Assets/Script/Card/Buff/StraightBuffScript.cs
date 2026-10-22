using System.Collections.Generic;
using static BattleRuntimeDefinitions;

// 1010 스트레이트: 한 턴에 비용 1·2·3 카드를 모두 쓰면 에너지 2와 카드 1장 (턴당 1회)
public sealed class StraightBuffScript : PlayerBuffScript
{
    private static readonly int[] RequiredCosts = { 1, 2, 3 };
    private const int EnergyAmount = 2;
    private const int DrawAmount = 1;

    private readonly HashSet<int> paidCostsThisTurn = new();
    private bool hasTriggeredThisTurn;

    public override int BuffId => StraightBuffId;

    // 툴팁 진행도: 필요한 비용 목록, 이번 턴 사용한 비용, 발동 여부
    public static IReadOnlyList<int> RequiredCostList => RequiredCosts;
    public bool HasPaidCostThisTurn(int cost) => paidCostsThisTurn.Contains(cost);
    public bool HasTriggeredThisTurn => hasTriggeredThisTurn;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        ResetTurn();
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        ResetTurn();
    }

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || player == null || playedCard == null || isRepeatedEffect || stack <= 0 || hasTriggeredThisTurn)
        {
            return;
        }

        paidCostsThisTurn.Add(battleManager.battleContext != null ? battleManager.battleContext.GetPaidCost(playedCard) : playedCard.cost);
        foreach (int requiredCost in RequiredCosts)
        {
            if (!paidCostsThisTurn.Contains(requiredCost))
            {
                return;
            }
        }

        hasTriggeredThisTurn = true;
        player.AddEnergy(EnergyAmount);
        battleManager.DrawCards(DrawAmount);
    }

    private void ResetTurn()
    {
        paidCostsThisTurn.Clear();
        hasTriggeredThisTurn = false;
    }
}
