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

        List<Card> processedCards = battleManager.ProcessGeneratedCards(BuffCardUtility.CreateRandomUniqueUpgradeCards(stack), true);
        if (processedCards.Count > 0 && battleManager.handManager != null)
        {
            battleManager.handManager.AddCard(processedCards);
            battleManager.UpdateAllUI();
        }
    }
}
