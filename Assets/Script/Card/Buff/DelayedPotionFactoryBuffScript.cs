using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class DelayedPotionFactoryBuffScript : PlayerBuffScript
{
    public override int BuffId => DelayedPotionFactoryBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || player == null || stack <= 0)
        {
            return;
        }

        // 상태 효과로 생성한 카드는 복제 생성 대상이 아님 (E-31)
        List<Card> processedCards = battleManager.ProcessGeneratedCards(BuffCardUtility.CreateRandomCardsFromGroup(BuffCardUtility.BasePotionGroupName, stack), false);
        if (processedCards.Count > 0 && battleManager.handManager != null)
        {
            battleManager.handManager.AddCard(processedCards);
            battleManager.UpdateAllUI();
        }

        // v1.0 제조는 다음 턴 한 번이 아니라 매 턴 시작 시 생성하므로 중첩을 소모하지 않음
    }
}
