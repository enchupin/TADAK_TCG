using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 모든 직업별 카드 덱을 관리하는 클래스
/// </summary>
public class SaveDeck : MonoBehaviour
{

    // 직업별 보유 카드 목록 -> 추후 데이터 베이스 연동으로 변경 예정
    public static Dictionary<Character, List<Card>> decksByCharacter = new Dictionary<Character, List<Card>>();
    
    private void Awake()
    {
        InitializeDecks();
    }



    // 특정 직업의 카드 목록 가져오기 -> 추후 데이터 베이스 연동으로 변경 예정
    public static List<Card> GetCards(Character character) {
        return new List<Card>(decksByCharacter[character]);
    }




    // 특정 직업의 카드 데이터 초기화
    public void ClearCards(Character character) {
        decksByCharacter[character].Clear();
    }

    // 모든 직업의 카드 데이터 초기화
    public void ClearAllCards() {
        foreach (var character in decksByCharacter.Keys) {
            decksByCharacter[character].Clear();
        }
    }





    
    // 특정 직업의 카드 수 가져오기
    public int GetCardCount(Character character) {
        return decksByCharacter[character].Count;
    }




    // 아마 ID로 관리될 예정일 듯 하여 추가 수정 필요
    // 특정 직업에 카드 추가
    public void AddCard(Character character, Card card)
    {
        if (card != null && !decksByCharacter[character].Contains(card))
        {
            decksByCharacter[character].Add(card);
        }
    }

    // 아마 ID로 관리될 예정일 듯 하여 추가 수정 필요
    // 특정 직업에서 카드 제거
    public bool RemoveCard(Character character, Card card)
    {
        return decksByCharacter[character].Remove(card);
    }


    // 아마 ID로 관리될 예정일 듯 하여 추가 수정 필요
    // 특정 직업의 카드 보유 여부 확인
    public bool HasCard(Character character, Card card)
    {
        return decksByCharacter[character].Contains(card);
    }





    // 덱 관리 변수 초기화
    private void InitializeDecks() {
        foreach (Character character in System.Enum.GetValues(typeof(Character))) {
            decksByCharacter[character] = new List<Card>();
        }
    }





}
