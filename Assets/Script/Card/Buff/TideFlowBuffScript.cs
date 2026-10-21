using static BattleRuntimeDefinitions;

// 3068 조류: 팽창 카드(카드 그룹 20404)를 쓸 때마다 물결 {stack}
public sealed class TideFlowBuffScript : PlayerBuffScript
{
    private const int SwellCardGroupId = 20404;

    public override int BuffId => TideFlowBuffId;

    public override void OnCardPlayed(TrainingBattleManager battleManager, PlayerData player, Card playedCard, Monster originalTarget, bool isRepeatedEffect, int stack)
    {
        if (battleManager == null || playedCard == null || isRepeatedEffect || stack <= 0 || playedCard.cardId / 10 != SwellCardGroupId)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(WaveBuffId, stack);
    }
}
