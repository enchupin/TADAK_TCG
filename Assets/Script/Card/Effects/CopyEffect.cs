using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CopyEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public MoveZoneType from;
    public MoveZoneType to;
    public MovePositionType position;
    public string subject;
    public List<int> cardIdList;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int amountOverride)
    {
        ExecuteInternal(battleManager, amountOverride);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int amountOverride)
    {
        if (battleManager == null || battleManager.usableDeckManager == null || battleManager.handManager == null) {
            return;
        }

        List<Card> sourceCards = ResolveSourceCards(battleManager);
        if (sourceCards.Count == 0) {
            return;
        }

        int copyCount = ResolveCopyCount(sourceCards.Count, battleManager, amountOverride);
        if (copyCount <= 0) {
            return;
        }

        List<Card> copiedCards = BuildCopiedCards(sourceCards, copyCount);
        if (copiedCards.Count == 0) {
            return;
        }

        copiedCards = battleManager.ProcessGeneratedCards(copiedCards);
        AttachCopies(copiedCards, battleManager);
        ExecuteOnCopiedCards(copiedCards, battleManager);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private List<Card> ResolveSourceCards(TrainingBattleManager battleManager)
    {
        List<Card> sourceCards = new List<Card>();
        MoveZoneType resolvedFrom = from == MoveZoneType.None ? MoveZoneType.Source : from;

        switch (resolvedFrom) {
            case MoveZoneType.Hand:
                List<Card> handCards = battleManager.handManager.GetHandCards();
                Card currentPlayedCard = battleManager.battleContext?.GetLastPlayedCard();
                foreach (Card card in handCards) {
                    if (card != null && card != currentPlayedCard) {
                        AddUnique(sourceCards, card);
                    }
                }
                break;
            case MoveZoneType.DrawPile:
                AddUnique(sourceCards, battleManager.usableDeckManager.GetDrawPile());
                break;
            case MoveZoneType.DiscardPile:
                AddUnique(sourceCards, battleManager.usableDeckManager.GetDiscardPile());
                break;
            case MoveZoneType.CardId:
                AddCardsByCardId(sourceCards);
                break;
            case MoveZoneType.Source:
                AddUnique(sourceCards, battleManager.battleContext?.GetSelectedCards());
                break;
        }

        return sourceCards;
    }

    private void AddCardsByCardId(List<Card> sourceCards)
    {
        if (sourceCards == null || cardIdList == null || cardIdList.Count == 0) {
            return;
        }

        foreach (int cardId in cardIdList) {
            Card card = CardManager.GetCardAsCard(cardId);
            if (card != null) {
                sourceCards.Add(card);
            }
        }
    }

    private int ResolveCopyCount(int sourceCount, TrainingBattleManager battleManager, int amountOverride)
    {
        if (sourceCount <= 0) {
            return 0;
        }

        if (IsAllFormula()) {
            return sourceCount;
        }

        if (!string.IsNullOrWhiteSpace(amountFormula)) {
            int evaluated = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, amountOverride);
            if (evaluated <= 0) {
                return 0;
            }
            return CanRepeatSourceCards() ? evaluated : Mathf.Min(evaluated, sourceCount);
        }

        if (amount > 0) {
            return CanRepeatSourceCards() ? amount : Mathf.Min(amount, sourceCount);
        }

        if (amountOverride > 0) {
            return CanRepeatSourceCards() ? amountOverride : Mathf.Min(amountOverride, sourceCount);
        }

        return CanRepeatSourceCards() ? sourceCount : 1;
    }

    private List<Card> BuildCopiedCards(List<Card> sourceCards, int copyCount)
    {
        List<Card> copiedCards = new List<Card>();
        if (sourceCards == null || sourceCards.Count == 0 || copyCount <= 0) {
            return copiedCards;
        }

        if (CanRepeatSourceCards()) {
            for (int i = 0; i < copyCount; i++) {
                Card source = sourceCards[i % sourceCards.Count];
                Card copied = CloneCard(source);
                if (copied != null) {
                    copiedCards.Add(copied);
                }
            }

            return copiedCards;
        }

        int uniqueCopyCount = Mathf.Clamp(copyCount, 0, sourceCards.Count);
        for (int i = 0; i < uniqueCopyCount; i++) {
            Card copied = CloneCard(sourceCards[i]);
            if (copied != null) {
                copiedCards.Add(copied);
            }
        }

        return copiedCards;
    }

    private static Card CloneCard(Card source)
    {
        return source?.CloneForRuntimeCopy();
    }

    private void AttachCopies(List<Card> copiedCards, TrainingBattleManager battleManager)
    {
        MoveZoneType resolvedTo = to == MoveZoneType.None ? MoveZoneType.Hand : to;

        switch (resolvedTo) {
            case MoveZoneType.Hand:
                battleManager.handManager.AddCard(copiedCards);
                return;
            case MoveZoneType.DiscardPile:
                battleManager.usableDeckManager.AddToDiscard(copiedCards);
                return;
            case MoveZoneType.DrawPile:
                if (position == MovePositionType.Top) {
                    for (int i = copiedCards.Count - 1; i >= 0; i--) {
                        battleManager.usableDeckManager.AddToDrawPileTop(copiedCards[i]);
                    }
                }
                else {
                    foreach (Card copiedCard in copiedCards) {
                        battleManager.usableDeckManager.AddToDrawPileRandom(copiedCard);
                    }

                    battleManager.battleContext?.OnDeckShuffled();
                }
                return;
            default:
                battleManager.handManager.AddCard(copiedCards);
                return;
        }
    }

    private void ExecuteOnCopiedCards(List<Card> copiedCards, TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null || copiedCards == null || copiedCards.Count == 0 || onActions == null) {
            return;
        }

        string contextSubject = string.IsNullOrWhiteSpace(subject) ? "GeneratedCard" : subject;
        battleManager.battleContext.SetContextCards(contextSubject, copiedCards);

        foreach (ICardEffect onAction in onActions) {
            onAction?.Execute(battleManager, copiedCards.Count);
        }

        battleManager.battleContext.ClearContextCards(contextSubject);
    }

    private bool IsSelectedSubject()
    {
        return string.Equals(subject, "Selected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subject, "SelectedCard", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanRepeatSourceCards()
    {
        if (IsSelectedSubject()) {
            return true;
        }

        return from == MoveZoneType.CardId;
    }

    private bool IsAllFormula()
    {
        return string.Equals(amountFormula, "all", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddUnique(List<Card> target, List<Card> source)
    {
        if (target == null || source == null) {
            return;
        }

        foreach (Card card in source) {
            AddUnique(target, card);
        }
    }

    private static void AddUnique(List<Card> target, Card card)
    {
        if (target == null || card == null) {
            return;
        }

        if (!target.Contains(card)) {
            target.Add(card);
        }
    }
}
