using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 훈련 모드(Run) 동안 유지되는 영구 덱 데이터
/// </summary>
public class BuildingDeck
{
    // 현재 보유한 모든 카드 리스트 (객체로 관리)
    private List<Card> deckList = new List<Card>();

    /// <summary>
    /// 캐릭터들의 기본 덱으로 초기화 (최초 1회)
    /// </summary>
    public void Initialize(List<Character> characters)
    {
        deckList.Clear();
        foreach (var character in characters)
        {
            CharacterData data = CharacterManager.GetCharacterByEnum(character);
            if (data != null && data.startDeckCardIds != null)
            {
                foreach (int cardId in data.startDeckCardIds)
                {
                    // ID로 새 Card 객체 생성하여 저장
                    Card newCard = CardManager.GetCardAsCard(cardId);
                    if (newCard != null)
                    {
                        deckList.Add(newCard);
                    }
                }
            }
        }
        Debug.Log($"[BuildingDeck] 초기화 완료: 총 {deckList.Count}장 (캐릭터 {characters.Count}명)");
    }

    /// <summary>
    /// 카드 추가 (보상 등)
    /// </summary>
    public void AddCard(int cardId)
    {
        // ID로 새 객체 생성
        Card newCard = CardManager.GetCardAsCard(cardId);
        if (newCard != null)
        {
            deckList.Add(newCard);
            Debug.Log($"[BuildingDeck] 카드 추가됨: {newCard.cardName} (총 {deckList.Count}장)");
        }
    }

    /// <summary>
    /// 카드 제거 (상점 등)
    /// </summary>
    public void RemoveCard(Card card)
    {
        if (deckList.Contains(card))
        {
            deckList.Remove(card);
            Debug.Log($"[BuildingDeck] 카드 제거됨: {card.cardName} (총 {deckList.Count}장)");
        }
        else
        {
            Debug.LogWarning($"[BuildingDeck] 제거할 카드가 덱에 없음: {card.cardName}");
        }
    }

    /// <summary>
    /// 전투용 덱 복사본 반환 (UsableDeck 생성용)
    /// </summary>
    public List<Card> CopyDeck()
    {
        return new List<Card>(deckList);
    }
}
