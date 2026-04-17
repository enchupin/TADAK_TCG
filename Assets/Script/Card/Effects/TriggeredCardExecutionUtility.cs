using System.Collections.Generic;

public static class TriggeredCardExecutionUtility
{
    public static void ExecuteTriggeredCard(
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
            return;
        }

        Monster resolvedTarget = ResolveTarget(battleManager, targetMonster);
        int repeatCount = allowRepeats ? battleManager.ConsumeRepeatedPlayCount(card, false) : 0;
        int cardUseAllEnemiesDamage = triggerPowerEffects ? battleManager.GetCardUseAllEnemiesDamage() : 0;

        ExecuteCardPlay(battleManager, card, resolvedTarget, true);

        if (triggerPowerEffects)
        {
            battleManager.HandlePlayedCardPowerEffects(card, resolvedTarget, false);

            if (cardUseAllEnemiesDamage > 0)
            {
                battleManager.ApplyCardUseAllEnemiesDamage(cardUseAllEnemiesDamage);
            }
        }

        for (int i = 0; i < repeatCount; i++)
        {
            ExecuteCardPlay(battleManager, card, resolvedTarget, false);

            if (triggerPowerEffects)
            {
                battleManager.HandlePlayedCardPowerEffects(card, resolvedTarget, true);
            }
        }

        if (resolveDestination)
        {
            ResolvePlayedCardDestination(battleManager, card);
        }

        if (applyPostPlayKeywords)
        {
            ApplyPostPlayKeywords(battleManager, card);
        }
    }

    private static void ExecuteCardPlay(TrainingBattleManager battleManager, Card card, Monster targetMonster, bool countAsPlayed)
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

        battleManager.currentTarget = targetMonster;
        card.Play(battleManager);
        battleManager.currentTarget = previousTarget;

        RestoreContext(context, "ThisCard", previousThisCards);
        RestoreContext(context, "Self", previousSelfCards);
        RestoreContext(context, "Selected", previousSelectedContextCards);
        RestoreContext(context, "SelectedCard", previousSelectedCardContextCards);
        context?.SetSelectedCards(previousSelectedCards);
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

    private static void ResolvePlayedCardDestination(TrainingBattleManager battleManager, Card playedCard)
    {
        if (battleManager == null || playedCard == null)
        {
            return;
        }

        if (battleManager.ShouldPotionGoToDiscardInsteadOfExhaust(playedCard))
        {
            battleManager.usableDeckManager?.AddToDiscard(playedCard);
            return;
        }

        if (playedCard.ShouldExhaustWhenPlayed())
        {
            battleManager.MoveCardToExhaust(playedCard);
            return;
        }

        if (battleManager.ShouldExhaustUnlockedUnplayableCard(playedCard))
        {
            battleManager.MoveCardToExhaust(playedCard);
            return;
        }

        if (playedCard.ShouldLeaveCombatWhenPlayed())
        {
            return;
        }

        if (HasResolvedCardDestination(battleManager, playedCard))
        {
            return;
        }

        battleManager.usableDeckManager?.AddToDiscard(playedCard);
    }

    private static bool HasResolvedCardDestination(TrainingBattleManager battleManager, Card playedCard)
    {
        if (playedCard == null || battleManager?.usableDeckManager == null)
        {
            return false;
        }

        return battleManager.usableDeckManager.GetDrawPile().Contains(playedCard)
            || battleManager.usableDeckManager.GetDiscardPile().Contains(playedCard)
            || battleManager.usableDeckManager.GetExhaustPile().Contains(playedCard);
    }

    private static void ApplyPostPlayKeywords(TrainingBattleManager battleManager, Card playedCard)
    {
        if (battleManager == null || playedCard == null)
        {
            return;
        }

        if (playedCard.HasKeyword(CardKeywordIds.Shadow))
        {
            CreateShadowCopy(battleManager, playedCard);
        }

        if (playedCard.HasKeyword(CardKeywordIds.Finale))
        {
            battleManager.ForceEndPlayerTurn();
        }
    }

    private static void CreateShadowCopy(TrainingBattleManager battleManager, Card sourceCard)
    {
        if (sourceCard == null || battleManager?.handManager == null)
        {
            return;
        }

        Card shadowCopy = sourceCard.CloneForRuntimeCopy();
        if (shadowCopy == null)
        {
            return;
        }

        shadowCopy.AddKeyword(CardKeywordIds.Ghost);
        shadowCopy.SetCost(UnityEngine.Mathf.Max(1, sourceCard.cost - 1), false);
        battleManager.handManager.AddCard(shadowCopy);
    }
}
