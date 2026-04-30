using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class GlacierShapeOnHitBuffScript : PlayerBuffScript
{
    private const int GlacierShapeCardId = 301080;

    public override int BuffId => GlacierShapeOnHitBuffId;

    public override void ResolveDeferredTurnStartEffects(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0 || battleManager.handManager == null || !battleManager.CanGainCardsToHand())
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

        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count <= 0)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }
}
