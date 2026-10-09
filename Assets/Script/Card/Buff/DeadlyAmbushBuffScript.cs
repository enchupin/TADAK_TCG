using static BattleRuntimeDefinitions;

public sealed class DeadlyAmbushBuffScript : PlayerBuffScript
{
    private bool hasConsumedThisTurn;
    private Card firstFeatherCard;
    private int firstFeatherBonus;

    public override int BuffId => DeadlyAmbushBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        hasConsumedThisTurn = false;
        firstFeatherCard = null;
        firstFeatherBonus = 0;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        ResetForCombat(battleManager, player);
    }

    public override void OnCardPlayStarted(TrainingBattleManager battleManager, PlayerData player, Card playedCard, int stack)
    {
        if (stack <= 0 || !BuffCardUtility.IsFeatherCard(playedCard)) return;
        if (!hasConsumedThisTurn)
        {
            hasConsumedThisTurn = true;
            firstFeatherCard = playedCard;
            firstFeatherBonus = stack;
        }
        else if (firstFeatherCard == playedCard)
        {
            firstFeatherCard = null;
        }
    }

    public override int GetCardBaseDamageBonus(TrainingBattleManager battleManager, PlayerData player, Card sourceCard, bool isAttackEffect, int stack, int currentBonus)
    {
        if (!isAttackEffect || stack <= 0 || !BuffCardUtility.IsFeatherCard(sourceCard)) return currentBonus;
        if (!hasConsumedThisTurn) return currentBonus + stack;

        // 최초 카드의 반복 실행에는 같은 보너스를 적용하고 사용 완료 후 미리보기에는 남기지 않음
        bool isFirstCardExecuting = firstFeatherCard == sourceCard
            && battleManager?.battleContext?.GetContextCard("ThisCard") == sourceCard;
        return isFirstCardExecuting ? currentBonus + firstFeatherBonus : currentBonus;
    }
}
