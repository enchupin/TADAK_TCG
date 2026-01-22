using System.Collections.Generic;
using UnityEngine;

public class DeckInitializer
{
    public List<Card> usableDeck;



    /// <summary>
    /// 선택된 캐릭터들의 카드를 가져와서 usableDeck을 초기화
    /// </summary>
    public void InitializeDeck()
    {

        // usableDeck 초기화
        usableDeck = new List<Card>();

        // 선택된 캐릭터가 없는 경우 체크
        if (SelectedButtonControl.selectedCharacterList == null || 
            SelectedButtonControl.selectedCharacterList.Count != 3)
        {
            Debug.LogWarning("캐릭터 선택이 잘못되었습니다!");
            return;
        }



        // 선택된 각 캐릭터의 카드를 가져와서 usableDeck에 추가
        foreach (Character character in SelectedButtonControl.selectedCharacterList)
        {
            // 추후 데이터 베이스 연결하는 방식으로 변경 필요
            List<Card> characterCards = SaveDeck.GetCards(character);
            

            if (characterCards != null && characterCards.Count > 0)
            {
                // 사용 덱에 카드 추가
                usableDeck.AddRange(characterCards);
            }
            else
            {
                // 예외 처리
                Debug.LogWarning($"{character} 직업의 카드가 없습니다!");
            }
        }
    }

}
