using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UpgradeCardsEffect : ICardEffect
{
    public MoveZoneType from = MoveZoneType.Source;
    public string subject;
    public int amount;
    public string amountFormula;
    public string upgrade;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        List<Card> targetCards = ResolveTargetCards(battleManager);
        if (targetCards.Count == 0)
        {
            return;
        }

        int remainingUpgradeCount = ResolveUpgradeCount(battleManager, targetCards.Count);
        List<Card> upgradedCards = new List<Card>();
        foreach (Card card in targetCards)
        {
            if (remainingUpgradeCount == 0)
            {
                break;
            }

            if (!CanUpgradeCard(card))
            {
                continue;
            }

            int upgradeCardId = ResolveUpgradeCardId(card);
            if (upgradeCardId <= 0)
            {
                continue;
            }

            Card upgradedTemplate = CardManager.GetCardAsCard(upgradeCardId);
            if (upgradedTemplate == null)
            {
                continue;
            }

            card.ApplyTemplate(upgradedTemplate);
            battleManager.ApplyPersistentUpgradeToCard(card);
            upgradedCards.Add(card);
            if (remainingUpgradeCount > 0)
            {
                remainingUpgradeCount--;
            }
        }

        if (upgradedCards.Count <= 0)
        {
            return;
        }

        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, upgradedCards);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private List<Card> ResolveTargetCards(TrainingBattleManager battleManager)
    {
        switch (from)
        {
            case MoveZoneType.Hand:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "Hand");
            case MoveZoneType.DrawPile:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "DrawPile");
            case MoveZoneType.DiscardPile:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, "DiscardPile");
            case MoveZoneType.AllCards:
                List<Card> allCards = new List<Card>();
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "Hand"));
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "DrawPile"));
                AddCards(allCards, CardEffectRuntimeUtility.ResolveCards(battleManager, "DiscardPile"));
                return allCards;
            case MoveZoneType.Source:
            case MoveZoneType.None:
                return CardEffectRuntimeUtility.ResolveCards(battleManager, null, subject);
            default:
                return new List<Card>();
        }
    }

    private int ResolveUpgradeCount(TrainingBattleManager battleManager, int targetCount)
    {
        if (string.Equals(amountFormula, "all", StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        int resolvedAmount = CardEffectRuntimeUtility.ResolveCardValueAmount(battleManager, amount, amountFormula, 0);
        if (resolvedAmount > 0)
        {
            return Mathf.Clamp(resolvedAmount, 0, targetCount);
        }

        return targetCount;
    }

    private int ResolveUpgradeCardId(Card card)
    {
        if (card?.enforceCardIds == null || card.enforceCardIds.Count == 0)
        {
            return 0;
        }

        if (string.Equals(upgrade, "Random", StringComparison.OrdinalIgnoreCase))
        {
            int randomIndex = UnityEngine.Random.Range(0, card.enforceCardIds.Count);
            return card.enforceCardIds[randomIndex];
        }

        return card.enforceCardIds[0];
    }

    private static bool CanUpgradeCard(Card card)
    {
        return card != null
            && Mathf.Abs(card.cardId) % 10 == 0
            && card.enforceCardIds != null
            && card.enforceCardIds.Count > 0;
    }

    private static void AddCards(List<Card> target, List<Card> source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (Card card in source)
        {
            if (card != null && !target.Contains(card))
            {
                target.Add(card);
            }
        }
    }
}
