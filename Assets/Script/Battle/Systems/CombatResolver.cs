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
    public bool IsResolvingCardPlay => isResolvingCardPlay;

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

        // 호기심 강화는 카드 사용이 아니므로 비용 · 사용 횟수 · 사용 시 효과 없이 강화만 하고 카드는 손으로 되돌림
        // 사용을 수락하지 않아 드래그한 카드가 손패 자리로 돌아감
        if (playedCard.isCurious)
        {
            isResolvingCardPlay = true;
            battleManager.UpdateEndTurnButtonState();
            battleManager.StartCoroutine(CardEffectSequence.Run(UpgradeCuriousCardSequence(playedCard), battleManager));
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

        eventData.MarkAccepted();
        controller.PrepareAcceptedPlayAnimationStart();
        battleManager.battleContext?.RecordPaidCost(playedCard, effectiveCost);

        int repeatCount = battleManager.ConsumeRepeatedPlayCount(playedCard, false);
        int cardUseAllEnemiesDamage = battleManager.GetCardUseAllEnemiesDamage();
        Debug.Log($"[Player] 카드 사용: {playedCard.cardName}");

        isResolvingCardPlay = true;
        try
        {
            battleManager.UpdateEndTurnButtonState();
            battleManager.battleContext?.OnCardPlayed(playedCard);
            battleManager.ChargeIdentityGaugeForPlayedCard(playedCard, effectiveCost);

            battleManager.StartCoroutine(CardEffectSequence.Run(
                FinishPlayedCardSequence(controller.cardUI, playedCard, eventData.targetMonster, repeatCount, cardUseAllEnemiesDamage), battleManager));
        }
        catch
        {
            battleManager.currentTarget = null;
            CompleteCardResolution();
            throw;
        }
    }

    // 사용 연출 상태 (선택 패널이 열리면 효과 처리 도중 먼저 시작)
    private class UseAnimationState
    {
        public CardUI CardUI;
        public bool Started;
        public bool Finished;
    }

    private IEnumerator FinishPlayedCardSequence(CardUI playedCardUI, Card playedCard, Monster originalTarget, int repeatCount, int cardUseAllEnemiesDamage)
    {
        UseAnimationState useState = new UseAnimationState { CardUI = playedCardUI };
        battleManager.StartCoroutine(StartUseAnimationWhenSelectionOpens(useState));
        try
        {
            battleManager.currentTarget = originalTarget;
            yield return playedCard.PlaySequence(battleManager);
            battleManager.currentTarget = null;
            battleManager.Buffs.Player.OnCardPlayed(playedCard, originalTarget, false);
            if (cardUseAllEnemiesDamage > 0)
                battleManager.ApplyCardUseAllEnemiesDamage(cardUseAllEnemiesDamage);

            while (battleManager.HasPendingSelection)
                yield return null;
            yield return ReplayCardEffectsIfNeeded(playedCard, originalTarget, repeatCount);
            battleManager.RefreshHandPlayableState();
            battleManager.UpdateAllUI();
            yield return null;

            useState.Finished = true;
            if (battleManager.handManager != null)
            {
                if (useState.Started)
                {
                    // 연출은 이미 진행 중이므로 손패 목록에서만 제거
                    battleManager.handManager.RemoveCard(playedCard);
                }
                else
                {
                    battleManager.handManager.RemoveCardFromHandWithUseAnimation(playedCardUI);
                }
            }

            // 사용 연출이 끝나기를 기다리지 않고 행선지 · 키워드를 바로 처리해 다음 카드를 곧바로 사용할 수 있게 함
            // 연출은 DrawUI에서 따로 진행되고 끝나면 카드 오브젝트를 정리
            CardPlayCompletion.ResolveDestination(battleManager, playedCard);
            if (battleManager.TryHandleCombatEnd())
            {
                yield break;
            }

            CardPlayCompletion.ApplyKeywords(battleManager, playedCard);
            battleManager.TryHandleCombatEnd();
        }
        finally
        {
            useState.Finished = true;
            battleManager.currentTarget = null;
            CompleteCardResolution();
        }
    }

    // 선택 패널이 열리면 사용된 카드가 화면에 멈춰 선택지를 가리지 않도록 사용 연출을 먼저 시작
    // 손패 목록 제거와 카드 이동 처리는 효과가 모두 끝난 뒤 그대로 진행
    private IEnumerator StartUseAnimationWhenSelectionOpens(UseAnimationState useState)
    {
        while (!useState.Finished)
        {
            if (battleManager.HasPendingSelection)
            {
                if (battleManager.handManager != null && useState.CardUI != null)
                {
                    useState.Started = true;
                    battleManager.handManager.PlayUseAnimation(useState.CardUI);
                }

                yield break;
            }

            yield return null;
        }
    }

    // 호기심 카드 강화: 강화 선택 후 손에 있는 카드 표시만 새로 고침
    private IEnumerator UpgradeCuriousCardSequence(Card curiousCard)
    {
        try
        {
            yield return battleManager.PlayCuriositySequence(curiousCard);
            // 행선지 고정용 표시는 사용 경로에서만 쓰므로 손에 남는 카드에서는 해제
            curiousCard.resolvedAsCuriosity = false;
            battleManager.handManager?.RefreshCardDisplay(curiousCard);
        }
        finally
        {
            CompleteCardResolution();
        }
    }

    private void CompleteCardResolution()
    {
        isResolvingCardPlay = false;
        battleManager.UpdateEndTurnButtonState();
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private IEnumerator ReplayCardEffectsIfNeeded(Card playedCard, Monster originalTarget, int repeatCount)
    {
        for (int i = 0; i < repeatCount; i++)
        {
            battleManager.currentTarget = originalTarget;
            yield return playedCard.PlaySequence(battleManager);
            battleManager.currentTarget = null;
            battleManager.Buffs.Player.OnCardPlayed(playedCard, originalTarget, true);
            while (battleManager.HasPendingSelection)
                yield return null;
        }
    }
}
