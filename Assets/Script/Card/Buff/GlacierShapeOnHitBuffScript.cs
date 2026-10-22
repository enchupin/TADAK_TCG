using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class GlacierShapeOnHitBuffScript : PlayerBuffScript
{
    private const int GlacierShapeCardId = 301080;

    public override int BuffId => GlacierShapeOnHitBuffId;

    public override void ResolveDeferredTurnStartEffects(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || player == null || stack <= 0)
        {
            return;
        }

        // v1.0 산산조각은 감소하지 않고 매 턴 시작 시 생성
        if (battleManager.handManager == null || !battleManager.CanGainCardsToHand())
        {
            return;
        }

        List<Card> generatedCards = new();
        for (int i = 0; i < stack; i++)
        {
            Card generatedCard = CardManager.GetCardAsCard(GlacierShapeCardId);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        // 상태 효과로 생성한 카드는 복제 생성 대상이 아님 (E-31)
        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, false);
        if (processedCards.Count <= 0)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }
}
