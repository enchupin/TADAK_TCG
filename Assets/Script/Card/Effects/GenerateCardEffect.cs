using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 생성 이펙트를 처리한다
/// </summary>
public class GenerateCardEffect : ICardEffect
{
    public List<RandomCardData> RandomCard; // 가중치 기반 랜덤 카드 목록
    public string cardId;
    public List<int> cardIdList;
    public TargetType target; // 생성 위치 (Hand, Discard, Deck)
    public MovePositionType position;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (!TryCreateCard(battleManager, out Card generatedCard))
        {
            return;
        }

        AddGeneratedCardToTarget(battleManager, generatedCard);
        if (battleManager != null)
        {
            battleManager.UpdateAllUI();
        }
    }

    public bool TryCreateCard(TrainingBattleManager battleManager, out Card generatedCard)
    {
        generatedCard = null;

        if (battleManager == null)
        {
            return false;
        }

        CardCollection collection = Resources.Load<CardCollection>("CardCollection");
        if (collection == null)
        {
            return false;
        }

        // 1. 생성할 카드 ID 선택
        int cardIdToGenerate = -1;

        if (RandomCard != null && RandomCard.Count > 0)
        {
            cardIdToGenerate = SelectRandomCard(collection);
        }
        else if (!string.IsNullOrWhiteSpace(cardId))
        {
            cardIdToGenerate = ResolveExplicitCardId(battleManager);
        }
        else if (cardIdList != null && cardIdList.Count > 0)
        {
            cardIdToGenerate = SelectRandomCardFromList(collection, cardIdList);
        }

        if (cardIdToGenerate == -1)
        {
            Debug.LogWarning("[GenerateCardEffect] 생성할 카드 ID를 선택하지 못해 생성 이펙트를 건너뜁니다.");
            return false;
        }

        // 2. 카드 데이터 조회
        CardData cardData = TryFindCardData(collection, cardIdToGenerate);
        if (cardData == null)
        {
            Debug.LogWarning($"[GenerateCardEffect] 카드 ID {cardIdToGenerate}를 찾을 수 없어 생성하지 않습니다.");
            return false;
        }

        // 3. 카드 인스턴스 생성
        generatedCard = cardData.ToCard();
        return generatedCard != null;
    }

    public void AddGeneratedCardToTarget(TrainingBattleManager battleManager, Card generatedCard)
    {
        if (battleManager == null || generatedCard == null)
        {
            return;
        }

        // 4. 지정 위치로 이동
        if (target == TargetType.Hand || target == TargetType.None) // 기본 위치는 Hand
        {
            if (battleManager.handManager != null)
            {
                battleManager.handManager.AddCard(generatedCard);
                Debug.Log($"[GenerateCardEffect] 생성된 카드: {generatedCard.cardName}");
            }
        }
        else if (target == TargetType.Discard)
        {
            if (battleManager.usableDeckManager != null)
            {
                battleManager.usableDeckManager.AddToDiscard(generatedCard);
                Debug.Log($"[GenerateCardEffect] 묘지로 이동한 카드: {generatedCard.cardName}");
            }
        }
        else if (target == TargetType.Deck)
        {
            // 덱으로 추가하는 경로가 생기면 여기에 구현
        }
    }

    private int SelectRandomCard(CardCollection collection)
    {
        int totalWeight = 0;
        foreach (RandomCardData randomCard in RandomCard)
        {
            if (randomCard != null && randomCard.weight > 0 && TryFindCardData(collection, randomCard.cardId) != null)
            {
                totalWeight += randomCard.weight;
            }
        }

        if (totalWeight <= 0)
        {
            Debug.LogWarning("[GenerateCardEffect] RandomCard에서 유효한 카드가 없어 생성하지 않습니다.");
            return -1;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (RandomCardData randomCard in RandomCard)
        {
            if (randomCard == null || randomCard.weight <= 0)
            {
                continue;
            }

            if (TryFindCardData(collection, randomCard.cardId) == null)
            {
                continue;
            }

            currentWeight += randomCard.weight;
            if (randomValue < currentWeight)
            {
                return randomCard.cardId;
            }
        }

        return -1;
    }

    private int SelectRandomCardFromList(CardCollection collection, List<int> candidateCardIds)
    {
        if (candidateCardIds == null || candidateCardIds.Count == 0)
        {
            return -1;
        }

        List<int> validIds = new();

        foreach (int candidateId in candidateCardIds)
        {
            if (TryFindCardData(collection, candidateId) != null)
            {
                validIds.Add(candidateId);
            }
        }

        if (validIds.Count == 0)
        {
            Debug.LogWarning("[GenerateCardEffect] cardIdList에서 유효한 카드가 없어 생성하지 않습니다.");
            return -1;
        }

        int index = Random.Range(0, validIds.Count);
        return validIds[index];
    }

    private CardData TryFindCardData(CardCollection collection, int targetCardId)
    {
        return collection != null
            ? collection.allCards.Find(c => c.cardId == targetCardId)
            : null;
    }

    private int ResolveExplicitCardId(TrainingBattleManager battleManager)
    {
        if (int.TryParse(cardId, out int resolvedId))
        {
            return resolvedId;
        }

        if (battleManager != null &&
            battleManager.battleContext != null &&
            (string.Equals(cardId, "SelectedCard", System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(cardId, "Selected", System.StringComparison.OrdinalIgnoreCase)))
        {
            List<Card> selectedCards = battleManager.battleContext.GetSelectedCards();
            if (selectedCards != null && selectedCards.Count > 0 && selectedCards[0] != null)
            {
                return selectedCards[0].cardId;
            }
        }

        return -1;
    }
}
