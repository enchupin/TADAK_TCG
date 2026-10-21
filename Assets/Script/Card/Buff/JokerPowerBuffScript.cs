using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class JokerPowerBuffScript : PlayerBuffScript
{
    public override int BuffId => JokerPowerBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        // 상태 효과로 생성한 카드는 복제 생성 대상이 아님 (E-31)
        List<Card> processedCards = battleManager.ProcessGeneratedCards(BuffCardUtility.CreateRandomUniqueUpgradeCards(stack, Character.Jack), false);
        if (processedCards.Count > 0 && battleManager.handManager != null)
        {
            battleManager.handManager.AddCard(processedCards);
            battleManager.UpdateAllUI();
        }
    }
}
