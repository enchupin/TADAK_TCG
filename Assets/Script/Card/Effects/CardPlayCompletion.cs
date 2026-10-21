// 직접 사용과 유발 실행에 공통인 행선지 및 사용 후 키워드 처리
public static class CardPlayCompletion
{
    public static void ResolveDestination(TrainingBattleManager battleManager, Card playedCard)
    {
        if (battleManager == null || playedCard == null)
        {
            return;
        }

        // 호기심으로 강화된 카드는 강화 계열의 키워드와 무관하게 버린 더미로 이동 (E-12)
        if (playedCard.resolvedAsCuriosity)
        {
            battleManager.usableDeckManager?.AddToDiscard(playedCard);
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

    public static void ApplyKeywords(TrainingBattleManager battleManager, Card playedCard)
    {
        if (battleManager == null || playedCard == null)
        {
            return;
        }

        if (playedCard.resolvedAsCuriosity)
        {
            playedCard.resolvedAsCuriosity = false;
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
        // 복제 카드는 원래 비용을 그대로 유지하며 최소 1 (R-21a)
        shadowCopy.SetCost(UnityEngine.Mathf.Max(1, sourceCard.baseCost), false);
        battleManager.handManager.AddCard(shadowCopy);
    }
}
