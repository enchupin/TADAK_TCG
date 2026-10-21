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

        // Choose: 강화 계열 중 선택지 3개를 제시해 고른 강화를 적용 (대상 1장)
        if (string.Equals(upgrade, "Choose", StringComparison.OrdinalIgnoreCase))
        {
            ChooseUpgrade(battleManager, targetCards.Find(CanUpgradeCard));
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

    private void ChooseUpgrade(TrainingBattleManager battleManager, Card card)
    {
        if (card == null)
        {
            return;
        }

        List<int> upgradeIds = new List<int>(card.enforceCardIds);
        List<Card> options = new List<Card>();
        while (upgradeIds.Count > 0 && options.Count < GameRules.Deck.choiceCount)
        {
            int index = UnityEngine.Random.Range(0, upgradeIds.Count);
            Card option = CardManager.GetCardAsCard(upgradeIds[index]);
            upgradeIds.RemoveAt(index);
            if (option != null)
            {
                options.Add(option);
            }
        }

        if (options.Count == 0)
        {
            return;
        }

        void Apply(List<Card> selected)
        {
            Card picked = selected != null && selected.Count > 0 ? selected[0] : options[0];
            card.ApplyTemplate(picked);
            battleManager.ApplyPersistentUpgradeToCard(card);
            CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, new List<Card> { card });
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
        }

        if (!battleManager.OpenSelectCardPanel(options, 1, Apply))
        {
            Apply(null);
        }
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
