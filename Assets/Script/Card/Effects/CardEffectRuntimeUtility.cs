using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 이펙트 공통 런타임 유틸리티
/// </summary>
public static class CardEffectRuntimeUtility
{
    public static List<Card> ResolveCards(TrainingBattleManager battleManager, string source, string subject = null)
    {
        List<Card> cards = new List<Card>();
        if (battleManager == null)
        {
            return cards;
        }

        string resolvedSource = string.IsNullOrWhiteSpace(source) ? subject : source;
        if (string.IsNullOrWhiteSpace(resolvedSource))
        {
            AddCard(cards, battleManager.battleContext?.GetContextCard("ThisCard"));
            if (cards.Count == 0)
            {
                AddCard(cards, battleManager.battleContext?.GetLastPlayedCard());
            }

            return cards;
        }

        switch (resolvedSource.ToLowerInvariant())
        {
            case "thiscard":
                AddCard(cards, battleManager.battleContext?.GetContextCard("ThisCard"));
                if (cards.Count == 0)
                {
                    AddCard(cards, battleManager.battleContext?.GetLastPlayedCard());
                }
                break;

            case "hand":
                List<Card> handCards = battleManager.handManager?.GetHandCards();
                Card currentPlayedCard = battleManager.battleContext?.GetLastPlayedCard();
                if (handCards != null)
                {
                    foreach (Card handCard in handCards)
                    {
                        if (handCard != null && handCard != currentPlayedCard)
                        {
                            AddCard(cards, handCard);
                        }
                    }
                }
                break;

            case "drawpile":
            case "deck":
                AddCards(cards, battleManager.usableDeckManager?.GetDrawPile());
                break;

            case "discardpile":
            case "discard":
                AddCards(cards, battleManager.usableDeckManager?.GetDiscardPile());
                break;

            default:
                AddCards(cards, battleManager.battleContext?.GetContextCards(resolvedSource));
                break;
        }

        if (cards.Count == 0 && !string.IsNullOrWhiteSpace(subject) && !string.Equals(resolvedSource, subject, System.StringComparison.OrdinalIgnoreCase))
        {
            AddCards(cards, battleManager.battleContext?.GetContextCards(subject));
        }

        return cards;
    }

    public static Card ResolveSingleCard(TrainingBattleManager battleManager, string source, string subject = null)
    {
        List<Card> cards = ResolveCards(battleManager, source, subject);
        return cards.Count > 0 ? cards[0] : null;
    }

    public static void RefreshCardDisplays(TrainingBattleManager battleManager, List<Card> cards)
    {
        if (battleManager?.handManager == null || cards == null || cards.Count == 0)
        {
            return;
        }

        battleManager.handManager.RefreshCardDisplays(cards);
    }

    public static int ResolveCardValueAmount(TrainingBattleManager battleManager, int amount, string amountFormula, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount);
        }

        if (amount != 0)
        {
            return amount;
        }

        return forwardedAmount;
    }

    public static Monster ResolveSingleEnemyTarget(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return null;
        }

        Monster target = battleManager.currentTarget;
        if (target != null && !target.IsDead())
        {
            return target;
        }

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters != null && livingMonsters.Count == 1)
        {
            return livingMonsters[0];
        }

        return null;
    }

    public static List<Monster> ResolveEnemyTargets(TrainingBattleManager battleManager, TargetType target)
    {
        List<Monster> targets = new List<Monster>();
        if (battleManager == null)
        {
            return targets;
        }

        switch (target)
        {
            case TargetType.SingleEnemy:
                Monster singleTarget = ResolveSingleEnemyTarget(battleManager);
                if (singleTarget != null && !singleTarget.IsDead())
                {
                    targets.Add(singleTarget);
                }
                break;

            case TargetType.AllEnemies:
                List<Monster> livingTargets = battleManager.GetLivingMonsters();
                if (livingTargets != null)
                {
                    foreach (Monster monster in livingTargets)
                    {
                        if (monster != null && !monster.IsDead())
                        {
                            targets.Add(monster);
                        }
                    }
                }
                break;
        }

        return targets;
    }

    public static int RemoveBuffStacks(List<Buff> buffs, int buffId, int amount, bool removeAllWhenAmountMissing)
    {
        if (buffs == null || buffId <= 0)
        {
            return 0;
        }

        Buff buff = buffs.Find(candidate => BuffIdMatches(candidate, buffId));
        if (buff == null)
        {
            return 0;
        }

        int removedAmount;
        if (amount > 0)
        {
            removedAmount = Mathf.Min(amount, buff.stack);
        }
        else if (removeAllWhenAmountMissing)
        {
            removedAmount = buff.stack;
        }
        else
        {
            removedAmount = 0;
        }

        if (removedAmount <= 0)
        {
            return 0;
        }

        buff.stack -= removedAmount;
        if (buff.stack <= 0)
        {
            buffs.Remove(buff);
        }

        return removedAmount;
    }

    public static int MultiplyBuffStacks(List<Buff> buffs, int buffId, int multiplier)
    {
        if (buffs == null || buffId <= 0 || multiplier < 0)
        {
            return 0;
        }

        Buff buff = buffs.Find(candidate => BuffIdMatches(candidate, buffId));
        if (buff == null)
        {
            return 0;
        }

        buff.stack = Mathf.Max(0, buff.stack * multiplier);
        if (buff.stack == 0)
        {
            buffs.Remove(buff);
        }

        return buff.stack;
    }

    public static Buff PickRandomBuffByFilters(List<Buff> buffs, List<int> allowedBuffFilters)
    {
        if (buffs == null || buffs.Count == 0 || allowedBuffFilters == null || allowedBuffFilters.Count == 0)
        {
            return null;
        }

        List<Buff> candidates = buffs.FindAll(buff =>
            buff != null &&
            buff.data != null &&
            buff.stack > 0 &&
            allowedBuffFilters.Exists(buff.data.MatchesAllowedType));

        if (candidates.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private static void AddCards(List<Card> target, List<Card> source)
    {
        if (target == null || source == null)
        {
            return;
        }

        foreach (Card card in source)
        {
            AddCard(target, card);
        }
    }

    private static void AddCard(List<Card> target, Card card)
    {
        if (target == null || card == null)
        {
            return;
        }

        if (!target.Contains(card))
        {
            target.Add(card);
        }
    }

    private static bool BuffIdMatches(Buff buff, int buffId)
    {
        return buff?.data != null
            && buff.data.buffId == buffId;
    }
}

[System.Serializable]
public class ExtraTurnEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null)
        {
            return;
        }

        int extraTurnCount = ResolveAmount(battleManager, forwardedAmount);
        if (extraTurnCount <= 0)
        {
            return;
        }

        battleManager.AddExtraTurn(extraTurnCount);
        Debug.Log($"[ExtraTurnEffect] 추가 턴 {extraTurnCount}회를 예약했습니다");
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return forwardedAmount > 0 ? forwardedAmount : 1;
    }
}
