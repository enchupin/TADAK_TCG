using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 훈련 모드(Run) 동안 유지되는 영구 덱 데이터
/// </summary>
public class BuildingDeck
{
    // 현재 보유한 모든 카드 ID 리스트
    private List<int> deckList = new List<int>();

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
                deckList.AddRange(data.startDeckCardIds);
            }
        }
        Debug.Log($"[BuildingDeck] 초기화 완료: 총 {deckList.Count}장 (캐릭터 {characters.Count}명)");
    }

    /// <summary>
    /// 카드 추가 (보상 등)
    /// </summary>
    public void AddCard(int cardId)
    {
        deckList.Add(cardId);
        Debug.Log($"[BuildingDeck] 카드 추가됨: {cardId} (총 {deckList.Count}장)");
    }

    /// <summary>
    /// 카드 제거 (상점 등)
    /// </summary>
    public void RemoveCard(int cardId)
    {
        if (deckList.Contains(cardId))
        {
            deckList.Remove(cardId);
            Debug.Log($"[BuildingDeck] 카드 제거됨: {cardId} (총 {deckList.Count}장)");
        }
        else
        {
            Debug.LogWarning($"[BuildingDeck] 제거할 카드가 덱에 없음: {cardId}");
        }
    }

    /// <summary>
    /// 전투용 덱 복사본 반환 (UsableDeck 생성용)
    /// </summary>
    public List<int> CopyDeck()
    {
        return new List<int>(deckList);
    }
}
