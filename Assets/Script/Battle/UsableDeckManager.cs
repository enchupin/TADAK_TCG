using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 사용 가능한 덱을 관리하는 매니저
/// </summary>
public class UsableDeckManager : MonoBehaviour
{
    public static UsableDeckManager Instance { get; private set; }
    public Queue<Card> usableDeck;

    private void Awake()
    {
        Initialize();
    }


    // 싱글톤 패턴
    private void Singleton() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }


    private void Initialize() {
        // 싱글톤
        Singleton();

        // usableDeck 초기화
        if (usableDeck == null) usableDeck = new Queue<Card>();

        // 저장 덱 불러오기
        InitializeDeck();
        


    }





    /// <summary>
    /// 덱에서 카드를 드로우 (O(1) 성능)
    /// </summary>
    public Card DrawCard() {
        if (usableDeck == null || usableDeck.Count == 0) {
            Debug.LogWarning("덱에 카드가 없습니다!");
            return null;
        }

        return usableDeck.Dequeue();
    }

    /// <summary>
    /// 덱 셔플
    /// </summary>
    public void ShuffleDeck() {
        if (usableDeck == null || usableDeck.Count == 0) {
            return;
        }

        // Queue를 List로 변환하여 셔플
        List<Card> tempList = new List<Card>(usableDeck);

        // Fisher-Yates 셔플 알고리즘
        for (int i = tempList.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        // 다시 Queue로 변환
        usableDeck = new Queue<Card>(tempList);
        Debug.Log("덱을 섞었습니다.");
    }

    /// <summary>
    /// 남은 카드 수를 반환
    /// </summary>
    public int GetRemainingCardCount() {
        return usableDeck != null ? usableDeck.Count : 0;
    }

    /// <summary>
    /// 선택된 캐릭터의 저장 덱 불러오기
    /// </summary>
    public void InitializeDeck() {
        // UsableDeckManager 체크
        if (UsableDeckManager.Instance == null) {
            Debug.LogError("UsableDeckManager를 찾을 수 없습니다!");
            return;
        }

        // usableDeck 초기화
        UsableDeckManager.Instance.usableDeck = new Queue<Card>();

        // 선택된 캐릭터가 없는 경우 체크
        if (SelectedButtonControl.selectedCharacterList == null ||
            SelectedButtonControl.selectedCharacterList.Count != 3) {
            Debug.LogWarning("캐릭터 선택이 잘못되었습니다! (3개의 캐릭터를 선택해야 합니다)");
            return;
        }

        // 선택된 각 캐릭터의 카드를 가져와서 usableDeck에 추가
        foreach (Character character in SelectedButtonControl.selectedCharacterList) {
            // 추후 데이터 베이스 연결하는 방식으로 변경 필요
            List<Card> characterCards = SaveDeck.GetCards(character);

            if (characterCards != null && characterCards.Count > 0) {
                // Queue에 카드 추가 (Enqueue 사용)
                foreach (Card card in characterCards) {
                    UsableDeckManager.Instance.usableDeck.Enqueue(card);
                }
                Debug.Log($"{character} 직업의 카드 {characterCards.Count}장을 덱에 추가했습니다.");
            } else {
                // 예외 처리
                Debug.LogWarning($"{character} 직업의 카드가 없습니다!");
            }
        }

        Debug.Log($"총 {UsableDeckManager.Instance.usableDeck.Count}장의 카드로 덱을 초기화했습니다.");
    }

}
