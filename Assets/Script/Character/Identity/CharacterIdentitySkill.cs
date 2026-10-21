using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterIdentitySkill
{
    public abstract bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState);

    protected static bool IsBattleReady(TrainingBattleManager battleManager)
    {
        return battleManager != null && battleManager.playerData != null;
    }

    protected static void AddCardsToHand(TrainingBattleManager battleManager, IEnumerable<int> cardIds)
    {
        if (battleManager?.handManager == null || cardIds == null)
        {
            return;
        }

        List<Card> generatedCards = new List<Card>();
        foreach (int cardId in cardIds)
        {
            Card generatedCard = CardManager.GetCardAsCard(cardId);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        if (generatedCards.Count == 0)
        {
            return;
        }

        generatedCards = battleManager.ProcessGeneratedCards(generatedCards);
        battleManager.handManager.AddCard(generatedCards);
    }

    // 이미 만든 카드를 생성 카드로 처리해 손에 추가 (복제 생성 등 생성 처리 적용)
    protected static void AddGeneratedCardsToHand(TrainingBattleManager battleManager, List<Card> cards)
    {
        if (battleManager?.handManager == null || cards == null || cards.Count == 0)
        {
            return;
        }

        battleManager.handManager.AddCard(battleManager.ProcessGeneratedCards(cards));
    }

    protected static void AddRepeatedCardToHand(TrainingBattleManager battleManager, int cardId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        List<int> cardIds = new List<int>(amount);
        for (int i = 0; i < amount; i++)
        {
            cardIds.Add(cardId);
        }

        AddCardsToHand(battleManager, cardIds);
    }

    protected static void AddRandomCardToHand(TrainingBattleManager battleManager, IReadOnlyList<int> candidateCardIds)
    {
        if (candidateCardIds == null || candidateCardIds.Count == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, candidateCardIds.Count);
        AddRepeatedCardToHand(battleManager, candidateCardIds[randomIndex], 1);
    }
}
