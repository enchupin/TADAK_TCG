using UnityEngine;

/// <summary>
/// Resolves a single card-play pipeline.
/// Handles validation, energy payment, effect execution, and card zone movement.
/// </summary>
public class CombatResolver
{
    private readonly TrainingBattleManager battleManager;

    public CombatResolver(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void TryPlayCard(CardPlayEventData eventData)
    {
        if (!battleManager.CanPlayerPlayCard())
        {
            Debug.LogWarning("[CombatResolver] Cannot play card right now. Not in player action state.");
            return;
        }

        if (eventData == null || eventData.cardController == null || eventData.cardController.Card == null)
        {
            Debug.LogWarning("[CombatResolver] Missing CardController or Card.");
            return;
        }

        CardController controller = eventData.cardController;
        Card playedCard = controller.Card;

        if (battleManager.playerData == null)
            return;

        int effectiveCost = battleManager.GetEffectiveCardCost(playedCard);

        if (!battleManager.CanPlayCard(playedCard))
        {
            Debug.LogWarning($"[CombatResolver] {playedCard.cardName} 카드는 현재 사용할 수 없습니다");
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        if (!battleManager.CanPayCardCost(playedCard))
        {
            Debug.LogWarning($"[CombatResolver] 카드 비용을 지불할 수 없습니다: {playedCard.cardName} ({effectiveCost})");
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        bool spent = battleManager.TryPayCardCost(playedCard);
        if (!spent)
        {
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        int repeatCount = battleManager.ConsumeRepeatedPlayCount(playedCard, false);
        int cardUseAllEnemiesDamage = battleManager.GetCardUseAllEnemiesDamage();
        Debug.Log($"[Player] 카드 사용: {playedCard.cardName}");

        battleManager.battleContext?.OnCardPlayed(playedCard);

        Monster originalTarget = eventData.targetMonster;
        battleManager.currentTarget = originalTarget;
        playedCard.Play(battleManager);
        battleManager.currentTarget = null;
        battleManager.HandlePlayedCardPowerEffects(playedCard, originalTarget, false);
        if (cardUseAllEnemiesDamage > 0)
        {
            battleManager.ApplyCardUseAllEnemiesDamage(cardUseAllEnemiesDamage);
        }
        ReplayCardEffectsIfNeeded(playedCard, originalTarget, repeatCount);

        if (battleManager.handManager != null)
        {
            battleManager.handManager.RemoveCardFromHand(controller.cardUI);
        }

        ResolvePlayedCardDestination(playedCard);
        if (battleManager.TryHandleCombatEnd())
        {
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            return;
        }

        ApplyPostPlayKeywords(playedCard);

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
        battleManager.TryHandleCombatEnd();
    }

    private void ResolvePlayedCardDestination(Card playedCard)
    {
        if (playedCard == null)
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

        battleManager.usableDeckManager?.AddToDiscard(playedCard);
    }

    private void ApplyPostPlayKeywords(Card playedCard)
    {
        if (playedCard == null)
        {
            return;
        }

        if (playedCard.HasKeyword(CardKeywordIds.Shadow))
        {
            CreateShadowCopy(playedCard);
        }

        if (playedCard.HasKeyword(CardKeywordIds.Finale))
        {
            battleManager.ForceEndPlayerTurn();
        }
    }

    private void CreateShadowCopy(Card sourceCard)
    {
        if (sourceCard == null || battleManager.handManager == null)
        {
            return;
        }

        Card shadowCopy = sourceCard.CloneForRuntimeCopy();
        if (shadowCopy == null)
        {
            return;
        }

        shadowCopy.AddKeyword(CardKeywordIds.Ghost);
        shadowCopy.SetCost(Mathf.Max(1, sourceCard.cost - 1), false);
        battleManager.handManager.AddCard(shadowCopy);
    }

    private void ReplayCardEffectsIfNeeded(Card playedCard, Monster originalTarget, int repeatCount)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            battleManager.currentTarget = originalTarget;
            playedCard.Play(battleManager);
            battleManager.currentTarget = null;
            battleManager.HandlePlayedCardPowerEffects(playedCard, originalTarget, true);
        }
    }
}
