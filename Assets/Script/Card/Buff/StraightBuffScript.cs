using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class StraightBuffScript : PlayerBuffScript
{
    private int pendingStraightDiscardCount;

    public override int BuffId => StraightBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        pendingStraightDiscardCount = 0;
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.AddTurnStartDrawModifier(stack * 2);
        pendingStraightDiscardCount += stack;
    }

    public override void ResolveDeferredTurnStartEffects(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || pendingStraightDiscardCount <= 0 || battleManager.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        List<Card> selectableCards = battleManager.handManager.GetHandCards();
        int discardCount = Mathf.Min(pendingStraightDiscardCount, selectableCards.Count);
        pendingStraightDiscardCount = 0;
        if (discardCount <= 0)
        {
            return;
        }

        bool opened = battleManager.OpenSelectCardPanel(selectableCards, discardCount, selectedCards => DiscardStraightCards(battleManager, selectedCards));
        if (opened)
        {
            return;
        }

        List<Card> fallbackSelection = new();
        for (int i = 0; i < discardCount; i++)
        {
            Card card = selectableCards[i];
            if (card != null)
            {
                fallbackSelection.Add(card);
            }
        }

        DiscardStraightCards(battleManager, fallbackSelection);
    }

    private static void DiscardStraightCards(TrainingBattleManager battleManager, List<Card> selectedCards)
    {
        if (battleManager?.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        int discardedCount = 0;
        List<Card> safeSelectedCards = selectedCards ?? new List<Card>();
        foreach (Card card in safeSelectedCards)
        {
            if (card == null || !battleManager.handManager.RemoveCard(card))
            {
                continue;
            }

            battleManager.usableDeckManager.AddToDiscard(card);
            discardedCount++;
        }

        if (discardedCount > 0)
        {
            battleManager.battleContext?.OnCardsDiscarded(discardedCount);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
