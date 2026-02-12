using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 생성 효과 (랜덤 또는 특정 리스트)
/// </summary>
public class GenerateCardEffect : ICardEffect
{
    public List<RandomCardData> RandomCard;  // 랜덤 카드 목록 (가중치 포함)
    public List<int> cardIdList; // 특정 카드 생성 시 사용
    public TargetType target; // 생성 위치 (Hand, Discard, Deck 등)
    
    public void Execute(TrainingBattleManager battleManager)
    {
        // 1. 생성할 카드 ID 결정
        int cardIdToGenerate = -1;
        
        if (RandomCard != null && RandomCard.Count > 0)
        {
            cardIdToGenerate = SelectRandomCard();
        }
        else if (cardIdList != null && cardIdList.Count > 0)
        {
            // TODO: 여러 장 생성 지원 시 반복문 필요. 현재는 첫 번째만.
            cardIdToGenerate = cardIdList[0]; 
        }
        
        if (cardIdToGenerate == -1)
        {
            Debug.LogWarning("[GenerateCardEffect] 생성할 카드가 정의되지 않았습니다.");
            return;
        }

        // 2. CardCollection에서 데이터 찾기
        CardCollection collection = Resources.Load<CardCollection>("CardCollection");
        if (collection == null) return;
        
        CardData cardData = collection.allCards.Find(c => c.cardId == cardIdToGenerate);
        if (cardData == null)
        {
            Debug.LogError($"[GenerateCardEffect] 카드 ID {cardIdToGenerate}를 찾을 수 없습니다.");
            return;
        }
        
        // 3. Card 객체 생성
        Card generatedCard = cardData.ToCard();
        
        // 4. 타겟 위치에 추가
        if (target == TargetType.Hand || target == TargetType.None) // Default to Hand
        {
            if (battleManager.handManager != null)
            {
                battleManager.handManager.AddCard(generatedCard);
                 Debug.Log($"[GenerateCardEffect] 손패에 카드 생성: {generatedCard.cardName}");
            }
        }
        else if (target == TargetType.Discard)
        {
            if (battleManager.usableDeckManager != null)
            {
                battleManager.usableDeckManager.AddToDiscard(generatedCard);
                Debug.Log($"[GenerateCardEffect] 버리기 더미에 카드 생성: {generatedCard.cardName}");
            }
        }
        else if (target == TargetType.Deck)
        {
             // 덱 맨 위/아래/랜덤 위치 추가 지원 필요
             // 현재 UsableDeckManager에 AddToDeck 기능 확인 필요
             // battleManager.usableDeckManager.AddToDeck(generatedCard); // TODO
             Debug.Log($"[GenerateCardEffect] 덱에 카드 생성 (미구현): {generatedCard.cardName}");
        }
        
        battleManager.UpdateAllUI();
    }
    
    private int SelectRandomCard()
    {
        int totalWeight = 0;
        foreach (var cardData in RandomCard) totalWeight += cardData.weight;
        
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var cardData in RandomCard)
        {
            currentWeight += cardData.weight;
            if (randomValue < currentWeight) return cardData.cardId;
        }
        return RandomCard[0].cardId;
    }
}
