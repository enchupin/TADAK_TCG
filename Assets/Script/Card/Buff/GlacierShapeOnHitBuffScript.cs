using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class GlacierShapeOnHitBuffScript : PlayerBuffScript
{
    private const int GlacierShapeCardId = 301080;

    private int pendingGlacierShapeCount;

    public override int BuffId => GlacierShapeOnHitBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        pendingGlacierShapeCount = 0;
    }

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (attacker == null || stack <= 0)
        {
            return;
        }

        pendingGlacierShapeCount += stack;
    }

    public override void ResolveDeferredTurnStartEffects(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || pendingGlacierShapeCount <= 0 || battleManager.handManager == null || !battleManager.CanGainCardsToHand())
        {
            return;
        }

        int generationCount = pendingGlacierShapeCount;
        pendingGlacierShapeCount = 0;

        List<Card> generatedCards = new();
        for (int i = 0; i < generationCount; i++)
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
