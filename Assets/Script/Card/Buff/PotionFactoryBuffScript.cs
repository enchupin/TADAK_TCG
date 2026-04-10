using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class PotionFactoryBuffScript : PlayerBuffScript
{
    public override int BuffId => PotionFactoryBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        AddGeneratedCardsToHand(battleManager, BuffCardUtility.CreateRandomCardsFromGroup(BuffCardUtility.BasePotionGroupName, stack));
    }

    private static void AddGeneratedCardsToHand(TrainingBattleManager battleManager, List<Card> generatedCards)
    {
        if (battleManager == null)
        {
            return;
        }

        List<Card> processedCards = battleManager.ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count == 0 || battleManager.handManager == null)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }
}
