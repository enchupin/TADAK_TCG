using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 덱 데이터 클래스
/// </summary>
[Serializable]
public class DeckData
{
    public string deckName; // 덱 이름
    public List<int> cardIds; // 카드 ID 리스트
    public DateTime createdDate; // 생성 날짜

    public DeckData(string name, List<int> cards)
    {
        deckName = name;
        cardIds = new List<int>(cards);
        createdDate = DateTime.Now;
    }
}

/// <summary>
/// 캐릭터별 덱 저장 관리 클래스
/// 각 캐릭터당 최대 3개의 덱을 저장
/// </summary>
public class SaveDeck : MonoBehaviour
{
    private const int MAX_DECKS_PER_CHARACTER = 3; // 캐릭터당 최대 덱 개수

    // 캐릭터별로 덱 리스트를 저장
    // Key: Character enum, Value: 해당 캐릭터의 덱 리스트 (최대 3개)
    private Dictionary<Character, List<DeckData>> decksByCharacter = new Dictionary<Character, List<DeckData>>();

    private void Awake()
    {
        // 모든 캐릭터에 대해 빈 덱 리스트 초기화
        foreach (Character character in Enum.GetValues(typeof(Character)))
        {
            if (!decksByCharacter.ContainsKey(character))
            {
                decksByCharacter[character] = new List<DeckData>();
            }
        }
    }

    /// <summary>
    /// 특정 캐릭터의 덱 개수 반환
    /// </summary>
    public int GetDeckCount(Character character)
    {
        if (decksByCharacter.ContainsKey(character))
        {
            return decksByCharacter[character].Count;
        }
        return 0;
    }

    /// <summary>
    /// 특정 캐릭터의 모든 덱 반환
    /// </summary>
    public List<DeckData> GetDecks(Character character)
    {
        if (decksByCharacter.ContainsKey(character))
        {
            return new List<DeckData>(decksByCharacter[character]);
        }
        return new List<DeckData>();
    }

    /// <summary>
    /// 특정 캐릭터의 특정 인덱스 덱 반환
    /// </summary>
    public DeckData GetDeck(Character character, int deckIndex)
    {
        if (decksByCharacter.ContainsKey(character) && 
            deckIndex >= 0 && 
            deckIndex < decksByCharacter[character].Count)
        {
            return decksByCharacter[character][deckIndex];
        }
        return null;
    }

    /// <summary>
    /// 새로운 덱 추가
    /// </summary>
    /// <returns>성공 여부</returns>
    public bool AddDeck(Character character, string deckName, List<int> cardIds)
    {
        if (!decksByCharacter.ContainsKey(character))
        {
            decksByCharacter[character] = new List<DeckData>();
        }

        // 최대 개수 확인
        if (decksByCharacter[character].Count >= MAX_DECKS_PER_CHARACTER)
        {
            Debug.LogWarning($"[SaveDeck] {character}는 이미 최대 덱 개수({MAX_DECKS_PER_CHARACTER})에 도달했습니다!");
            return false;
        }

        DeckData newDeck = new DeckData(deckName, cardIds);
        decksByCharacter[character].Add(newDeck);
        Debug.Log($"[SaveDeck] {character}에 덱 '{deckName}' 추가 완료 (카드 {cardIds.Count}장)");
        return true;
    }

    /// <summary>
    /// 덱 삭제
    /// </summary>
    public bool RemoveDeck(Character character, int deckIndex)
    {
        if (decksByCharacter.ContainsKey(character) && 
            deckIndex >= 0 && 
            deckIndex < decksByCharacter[character].Count)
        {
            string deckName = decksByCharacter[character][deckIndex].deckName;
            decksByCharacter[character].RemoveAt(deckIndex);
            Debug.Log($"[SaveDeck] {character}의 덱 '{deckName}' 삭제 완료");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 덱 업데이트 (덱 슬롯에 덱 덮어쓰기)
    /// </summary>
    public bool UpdateDeck(Character character, int deckIndex, string deckName, List<int> cardIds)
    {
        if (decksByCharacter.ContainsKey(character) && 
            deckIndex >= 0 && 
            deckIndex < decksByCharacter[character].Count)
        {
            decksByCharacter[character][deckIndex] = new DeckData(deckName, cardIds);
            Debug.Log($"[SaveDeck] {character}의 덱 인덱스 {deckIndex} 업데이트 완료");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 특정 캐릭터의 모든 덱 삭제
    /// </summary>
    public void ClearDecks(Character character)
    {
        if (decksByCharacter.ContainsKey(character))
        {
            decksByCharacter[character].Clear();
            Debug.Log($"[SaveDeck] {character}의 모든 덱 삭제 완료");
        }
    }
}
