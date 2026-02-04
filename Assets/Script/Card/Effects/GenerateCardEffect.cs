using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 카드 생성 효과 (랜덤)
/// </summary>
public class GenerateCardEffect : ICardEffect
{
    public List<RandomCardData> RandomCard;  // 랜덤 카드 목록 (가중치 포함)
    
    public void Execute(TrainingBattleManager battleManager)
    {
        if (RandomCard == null || RandomCard.Count == 0)
        {
            Debug.LogWarning("[GenerateCardEffect] RandomCard 목록이 비어있습니다.");
            return;
        }
        
        // 가중치 기반 랜덤 선택
        int selectedCardId = SelectRandomCard();
        
        // CardCollection에서 카드 찾기
        CardCollection collection = Resources.Load<CardCollection>("CardCollection");
        if (collection == null)
        {
            Debug.LogError("[GenerateCardEffect] CardCollection을 찾을 수 없습니다.");
            return;
        }
        
        CardData cardData = collection.allCards.Find(c => c.cardId == selectedCardId);
        if (cardData == null)
        {
            Debug.LogError($"[GenerateCardEffect] 카드 ID {selectedCardId}를 찾을 수 없습니다.");
            return;
        }
        
        // Card 객체 생성
        Card generatedCard = cardData.ToCard();
        
        // HandManager를 통해 손패에 추가
        HandManager handManager = battleManager.GetComponent<HandManager>();
        if (handManager == null)
        {
            handManager = battleManager.GetComponentInChildren<HandManager>();
        }
        
        if (handManager != null)
        {
            handManager.AddCard(generatedCard);
            Debug.Log($"[GenerateCardEffect] 카드 생성: {generatedCard.cardName} (ID: {selectedCardId})");
        }
        else
        {
            Debug.LogError("[GenerateCardEffect] HandManager를 찾을 수 없습니다.");
        }
        
        battleManager.UpdateAllUI();
    }
    
    private int SelectRandomCard()
    {
        // 총 가중치 계산
        int totalWeight = 0;
        foreach (var cardData in RandomCard)
        {
            totalWeight += cardData.weight;
        }
        
        // 랜덤 값 생성
        int randomValue = Random.Range(0, totalWeight);
        
        // 가중치 기반 선택
        int currentWeight = 0;
        foreach (var cardData in RandomCard)
        {
            currentWeight += cardData.weight;
            if (randomValue < currentWeight)
            {
                return cardData.cardId;
            }
        }
        
        // 기본값 (첫 번째 카드)
        return RandomCard[0].cardId;
    }
}
