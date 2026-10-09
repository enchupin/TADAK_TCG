using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class GrowingFeatherBuffScript : PlayerBuffScript
{
    public override int BuffId => GrowingFeatherBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        List<Card> generatedCards = new();
        for (int i = 0; i < stack; i++)
        {
            Card card = CardManager.GetCardAsCard(203080);
            if (card != null) generatedCards.Add(card);
        }

        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count > 0 && battleManager.handManager != null)
        {
            battleManager.handManager.AddCard(processedCards);
            battleManager.UpdateAllUI();
        }
    }
}
