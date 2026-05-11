using System.Collections;
using UnityEngine;

/// <summary>
/// Resolves a single card-play pipeline.
/// Handles validation, energy payment, effect execution, and card zone movement.
/// </summary>
public class CombatResolver
{
    private readonly TrainingBattleManager battleManager;
    private bool isResolvingCardPlay;

    public CombatResolver(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void TryPlayCard(CardPlayEventData eventData)
    {
        if (isResolvingCardPlay)
        {
            Debug.LogWarning("[CombatResolver] 카드 처리 중에는 다른 카드를 사용할 수 없습니다");
            return;
        }

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

        isResolvingCardPlay = true;
        battleManager.battleContext?.OnCardPlayed(playedCard);
        battleManager.ChargeIdentityGauge(playedCard.character);

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

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
        battleManager.StartCoroutine(FinishPlayedCardSequence(controller.cardUI, playedCard));
    }

    private IEnumerator FinishPlayedCardSequence(CardUI playedCardUI, Card playedCard)
    {
        yield return null;

        Coroutine useAnimation = null;
        if (battleManager.handManager != null)
        {
            useAnimation = battleManager.handManager.RemoveCardFromHandWithUseAnimation(playedCardUI);
        }

        if (useAnimation != null)
        {
            yield return useAnimation;
        }

        ResolvePlayedCardDestination(playedCard);
        if (battleManager.TryHandleCombatEnd())
        {
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            isResolvingCardPlay = false;
            yield break;
        }

        ApplyPostPlayKeywords(playedCard);

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
        battleManager.TryHandleCombatEnd();
        isResolvingCardPlay = false;
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
            battleManager.RemoveCardFromCombat(playedCard);
            return;
        }

        if (HasResolvedCardDestination(playedCard))
        {
            return;
        }

        battleManager.usableDeckManager?.AddToDiscard(playedCard);
    }

    private bool HasResolvedCardDestination(Card playedCard)
    {
        if (playedCard == null || battleManager?.usableDeckManager == null)
        {
            return false;
        }

        return battleManager.usableDeckManager.GetDrawPile().Contains(playedCard)
            || battleManager.usableDeckManager.GetDiscardPile().Contains(playedCard)
            || battleManager.usableDeckManager.GetExhaustPile().Contains(playedCard);
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
