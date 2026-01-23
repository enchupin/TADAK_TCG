using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 모든 직업별 카드 덱을 관리하는 클래스
/// 카드 ID와 보유 수량을 관리합니다.
/// </summary>
public class SaveDeck : MonoBehaviour
{

    // 직업별 보유 카드 목록 (카드 ID, 보유 수량) -> 추후 데이터 베이스 연동으로 변경 예정
    public static Dictionary<Character, Dictionary<int, int>> decksByCharacter = new Dictionary<Character, Dictionary<int, int>>();
    
    private void Awake()
    {
        InitializeDecks();
    }



    // 특정 직업의 카드 목록 가져오기 (수량만큼 카드 ID를 반복하여 반환) -> 추후 데이터 베이스 연동으로 변경 예정
    public static List<int> GetCards(Character character) {
        List<int> cardList = new List<int>();
        
        if (decksByCharacter.ContainsKey(character))
        {
            foreach (var cardEntry in decksByCharacter[character])
            {
                int cardId = cardEntry.Key;
                int quantity = cardEntry.Value;
                
                // 보유 수량만큼 카드 ID를 리스트에 추가
                for (int i = 0; i < quantity; i++)
                {
                    cardList.Add(cardId);
                }
            }
        }
        
        return cardList;
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





    
    // 특정 직업의 총 카드 수 가져오기 (모든 카드의 수량 합계)
    public int GetCardCount(Character character) {
        int totalCount = 0;
        
        if (decksByCharacter.ContainsKey(character))
        {
            foreach (var cardEntry in decksByCharacter[character])
            {
                totalCount += cardEntry.Value;
            }
        }
        
        return totalCount;
    }


    // 특정 직업의 고유 카드 종류 수 가져오기
    public int GetUniqueCardCount(Character character) {
        if (decksByCharacter.ContainsKey(character))
        {
            return decksByCharacter[character].Count;
        }
        return 0;
    }



    // 특정 직업에 카드 한 장 추가
    public void AddCard(Character character, int cardId)
    {
        AddCard(character, cardId, 1);
    }

    // 특정 직업에 카드 추가
    public void AddCard(Character character, int cardId, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning($"카드 추가 실패: 수량은 1 이상이어야 합니다. (입력된 수량: {quantity})");
            return;
        }

        if (decksByCharacter.ContainsKey(character))
        {
            if (decksByCharacter[character].ContainsKey(cardId))
            {
                // 이미 보유한 카드라면 수량 증가
                decksByCharacter[character][cardId] += quantity;
            }
            else
            {
                // 새로운 카드 추가
                decksByCharacter[character][cardId] = quantity;
            }
        }
    }

    // 특정 직업에서 카드 한 장 제거
    public bool RemoveCard(Character character, int cardId)
    {
        return RemoveCard(character, cardId, 1);
    }

    // 특정 직업에서 카드 제거
    public bool RemoveCard(Character character, int cardId, int quantity)
    {
        if (quantity <= 0)
        {
            Debug.LogWarning($"카드 제거 실패: 수량은 1 이상이어야 합니다. (입력된 수량: {quantity})");
            return false;
        }

        if (decksByCharacter.ContainsKey(character) && decksByCharacter[character].ContainsKey(cardId))
        {
            int currentQuantity = decksByCharacter[character][cardId];
            
            if (currentQuantity > quantity)
            {
                // 수량 감소
                decksByCharacter[character][cardId] -= quantity;
                return true;
            }
            else if (currentQuantity == quantity)
            {
                // 수량이 정확히 일치하면 카드 완전 제거
                decksByCharacter[character].Remove(cardId);
                return true;
            }
            else
            {
                Debug.LogWarning($"카드 제거 실패: 보유 수량({currentQuantity})보다 많은 수량({quantity})을 제거하려고 했습니다.");
                return false;
            }
        }
        
        return false;
    }


    // 특정 직업의 카드 보유 여부 확인
    public bool HasCard(Character character, int cardId)
    {
        return decksByCharacter.ContainsKey(character) && decksByCharacter[character].ContainsKey(cardId);
    }

    // 특정 직업의 특정 카드 보유 수량 확인
    public int GetCardQuantity(Character character, int cardId)
    {
        if (decksByCharacter.ContainsKey(character) && decksByCharacter[character].ContainsKey(cardId))
        {
            return decksByCharacter[character][cardId];
        }
        return 0;
    }

    // 특정 직업의 모든 카드 정보 가져오기 (카드 ID와 수량)
    public Dictionary<int, int> GetCardDictionary(Character character)
    {
        if (decksByCharacter.ContainsKey(character))
        {
            return new Dictionary<int, int>(decksByCharacter[character]);
        }
        return new Dictionary<int, int>();
    }






    // 덱 관리 변수 초기화
    private void InitializeDecks() {
        foreach (Character character in System.Enum.GetValues(typeof(Character))) {
            decksByCharacter[character] = new Dictionary<int, int>();
        }
    }




}
