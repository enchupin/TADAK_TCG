using System.Collections;
using System.Collections.Generic;

public static class TriggeredCardExecutionUtility
{
    public static IEnumerator ExecuteTriggeredCardSequence(
        TrainingBattleManager battleManager,
        Card card,
        Monster targetMonster = null,
        bool resolveDestination = false,
        bool triggerPowerEffects = false,
        bool allowRepeats = false,
        bool applyPostPlayKeywords = false)
    {
        if (battleManager == null || card == null)
        {
            yield break;
        }

        Monster resolvedTarget = ResolveTarget(battleManager, targetMonster);
        int repeatCount = allowRepeats ? battleManager.ConsumeRepeatedPlayCount(card, false) : 0;
        int cardUseAllEnemiesDamage = triggerPowerEffects ? battleManager.GetCardUseAllEnemiesDamage() : 0;

        yield return ExecuteCardPlay(battleManager, card, resolvedTarget, true);

        if (triggerPowerEffects)
        {
            battleManager.Buffs.Player.OnCardPlayed(card, resolvedTarget, false);

            if (cardUseAllEnemiesDamage > 0)
            {
                battleManager.ApplyCardUseAllEnemiesDamage(cardUseAllEnemiesDamage);
            }
        }

        for (int i = 0; i < repeatCount; i++)
        {
            while (battleManager.HasPendingSelection)
                yield return null;
            yield return ExecuteCardPlay(battleManager, card, resolvedTarget, false);

            if (triggerPowerEffects)
            {
                battleManager.Buffs.Player.OnCardPlayed(card, resolvedTarget, true);
            }
        }

        while (battleManager.HasPendingSelection)
            yield return null;

        if (resolveDestination)
        {
            CardPlayCompletion.ResolveDestination(battleManager, card);
        }

        if (applyPostPlayKeywords)
        {
            CardPlayCompletion.ApplyKeywords(battleManager, card);
        }
    }

    private static IEnumerator ExecuteCardPlay(TrainingBattleManager battleManager, Card card, Monster targetMonster, bool countAsPlayed)
    {
        BattleContext context = battleManager?.battleContext;
        List<Card> previousThisCards = context?.GetContextCards("ThisCard") ?? new List<Card>();
        List<Card> previousSelfCards = context?.GetContextCards("Self") ?? new List<Card>();
        List<Card> previousSelectedCards = context?.GetSelectedCards() ?? new List<Card>();
        List<Card> previousSelectedContextCards = context?.GetContextCards("Selected") ?? new List<Card>();
        List<Card> previousSelectedCardContextCards = context?.GetContextCards("SelectedCard") ?? new List<Card>();
        Monster previousTarget = battleManager.currentTarget;

        if (countAsPlayed)
        {
            context?.OnCardPlayed(card);
        }

        try
        {
            battleManager.currentTarget = targetMonster;
            yield return card.PlaySequence(battleManager);
        }
        finally
        {
            battleManager.currentTarget = previousTarget;

            RestoreContext(context, "ThisCard", previousThisCards);
            RestoreContext(context, "Self", previousSelfCards);
            RestoreContext(context, "Selected", previousSelectedContextCards);
            RestoreContext(context, "SelectedCard", previousSelectedCardContextCards);
            context?.SetSelectedCards(previousSelectedCards);
        }
    }

    private static void RestoreContext(BattleContext context, string subject, List<Card> cards)
    {
        if (context == null)
        {
            return;
        }

        if (cards == null || cards.Count == 0)
        {
            context.ClearContextCards(subject);
            return;
        }

        context.SetContextCards(subject, cards);
    }

    private static Monster ResolveTarget(TrainingBattleManager battleManager, Monster targetMonster)
    {
        if (targetMonster != null && !targetMonster.IsDead())
        {
            return targetMonster;
        }

        if (battleManager?.currentTarget != null && !battleManager.currentTarget.IsDead())
        {
            return battleManager.currentTarget;
        }

        List<Monster> livingMonsters = battleManager?.GetLivingMonsters();
        return livingMonsters != null && livingMonsters.Count == 1
            ? livingMonsters[0]
            : null;
    }

}
