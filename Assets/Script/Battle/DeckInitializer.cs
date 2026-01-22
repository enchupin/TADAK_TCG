using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택된 캐릭터들의 카드를 가져와서 UsableDeckManager의 덱을 초기화하는 클래스
/// </summary>
public class DeckInitializer
{
    /// <summary>
    /// 선택된 캐릭터들의 카드를 가져와서 UsableDeckManager의 usableDeck을 초기화
    /// </summary>
    public void InitializeDeck()
    {
        // UsableDeckManager 체크
        if (UsableDeckManager.Instance == null)
        {
            Debug.LogError("UsableDeckManager를 찾을 수 없습니다!");
            return;
        }

        // usableDeck 초기화
        UsableDeckManager.Instance.usableDeck = new Queue<Card>();

        // 선택된 캐릭터가 없는 경우 체크
        if (SelectedButtonControl.selectedCharacterList == null || 
            SelectedButtonControl.selectedCharacterList.Count != 3)
        {
            Debug.LogWarning("캐릭터 선택이 잘못되었습니다! (3개의 캐릭터를 선택해야 합니다)");
            return;
        }

        // 선택된 각 캐릭터의 카드를 가져와서 usableDeck에 추가
        foreach (Character character in SelectedButtonControl.selectedCharacterList)
        {
            // 추후 데이터 베이스 연결하는 방식으로 변경 필요
            List<Card> characterCards = SaveDeck.GetCards(character);
            
            if (characterCards != null && characterCards.Count > 0)
            {
                // Queue에 카드 추가 (Enqueue 사용)
                foreach (Card card in characterCards)
                {
                    UsableDeckManager.Instance.usableDeck.Enqueue(card);
                }
                Debug.Log($"{character} 직업의 카드 {characterCards.Count}장을 덱에 추가했습니다.");
            }
            else
            {
                // 예외 처리
                Debug.LogWarning($"{character} 직업의 카드가 없습니다!");
            }
        }

        Debug.Log($"총 {UsableDeckManager.Instance.usableDeck.Count}장의 카드로 덱을 초기화했습니다.");
    }
}
