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

        List<Card> processedCards = battleManager.ProcessGeneratedCards(BuffCardUtility.CreateRandomCardsFromGroup(BuffCardUtility.BasePotionGroupName, stack), true);
        if (processedCards.Count > 0 && battleManager.handManager != null)
        {
            battleManager.handManager.AddCard(processedCards);
            battleManager.UpdateAllUI();
        }

        player.ConsumeBuffStack(BuffId, stack);
    }
}
