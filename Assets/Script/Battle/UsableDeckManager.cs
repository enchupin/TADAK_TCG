using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 현재 사용 가능한 덱을 관리하는 매니저
/// </summary>
public class UsableDeckManager : MonoBehaviour
{
    public Queue<int> usableDeck;
    public List<int> discardPile = new List<int>(); // 버린 카드 더미


    /// <summary>
    /// 버리기 더미에 카드 추가
    /// </summary>
    public void AddToDiscard(int cardId)
    {
        discardPile.Add(cardId);
    }

    /// <summary>
    /// 버리기 더미에 카드 리스트 추가
    /// </summary>
    public void AddToDiscard(List<int> cardIds)
    {
        discardPile.AddRange(cardIds);
    }







    /// <summary>
    /// 덱에서 카드 1장을 드로우
    /// </summary>
    public int DrawCard() {
        if (usableDeck == null || usableDeck.Count == 0) {
            Debug.LogWarning("덱에 카드가 없습니다!");
            return 0;
        }

        return usableDeck.Dequeue();
    }

    /// <summary>
    /// 덱에서 지정된 수만큼 카드를 드로우
    /// </summary>
    public List<int> DrawCard(int count) {
        List<int> drawnCards = new List<int>();

        if (usableDeck == null || usableDeck.Count == 0) {
            Debug.LogWarning("덱에 카드가 없습니다!");
            return drawnCards;
        }

        // 요청한 수와 실제 덱에 남은 카드 수 중 작은 값만큼 드로우
        int actualDrawCount = Mathf.Min(count, usableDeck.Count);

        for (int i = 0; i < actualDrawCount; i++) {
            drawnCards.Add(usableDeck.Dequeue());
        }

        if (actualDrawCount < count) {
            Debug.LogWarning($"덱에 {count}장을 요청했지만 {actualDrawCount}장만 드로우했습니다.");
        }

        return drawnCards;
    }





    /// <summary>
    /// 덱 셔플
    /// </summary>
    public void ShuffleDeck() {
        if (usableDeck == null || usableDeck.Count == 0) {
            return;
        }

        // Queue를 List로 변환하여 셔플
        List<int> tempList = new List<int>(usableDeck);

        // Fisher-Yates 셔플 알고리즘
        for (int i = tempList.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            int temp = tempList[i];
            tempList[i] = tempList[randomIndex];
            tempList[randomIndex] = temp;
        }

        // 다시 Queue로 변환
        usableDeck = new Queue<int>(tempList);
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

        // usableDeck 초기화
        usableDeck = new Queue<int>();

        // 선택된 캐릭터가 없는 경우 체크
        if (SelectedButtonControl.selectedCharacterList == null ||
            SelectedButtonControl.selectedCharacterList.Count != 3) {
            Debug.LogWarning("캐릭터 선택이 잘못되었습니다! (3개의 캐릭터를 선택해야 합니다)");

            // 테스트 용: Chloe, Ignia, Declan 3명 선택
            SelectedButtonControl.selectedCharacterList.Clear();
            SelectedButtonControl.selectedCharacterList.Add(Character.Chloe);
            SelectedButtonControl.selectedCharacterList.Add(Character.Ignia);
            SelectedButtonControl.selectedCharacterList.Add(Character.Declan);
        }
        
        Debug.Log($"[UsableDeckManager] 선택된 캐릭터: {SelectedButtonControl.selectedCharacterList.Count}명");

        // ✅ 훈련 모드: CharacterManager를 이용해 각 캐릭터의 StartDeck 로드
        foreach (Character characterEnum in SelectedButtonControl.selectedCharacterList) {
            // CharacterManager를 통해 데이터 로드
            CharacterData charData = CharacterManager.GetCharacterByEnum(characterEnum);

            if (charData != null) {
                // StartDeck (기본 덱) ID 리스트 가져오기
                List<int> startDeckIds = charData.startDeckCardIds;
                
                if (startDeckIds != null && startDeckIds.Count > 0) {
                    foreach (int cardId in startDeckIds) {
                        // 카드 유효성 검사 (CardManager에 존재하는지)
                        if (CardManager.GetCard(cardId) != null) {
                            usableDeck.Enqueue(cardId);
                        } else {
                            Debug.LogWarning($"[UsableDeckManager] Card ID {cardId} not found in CardManager!");
                        }
                    }
                    Debug.Log($"[UsableDeckManager] {characterEnum} ({charData.characterName})의 기본 덱 {startDeckIds.Count}장을 추가했습니다.");
                } else {
                     Debug.LogWarning($"[UsableDeckManager] {characterEnum}의 StartDeck이 비어있습니다!");
                }
            } else {
                Debug.LogWarning($"[UsableDeckManager] CharacterData for {characterEnum} not found! CharacterManager가 초기화되었는지 확인하세요.");
            }
        }

        Debug.Log($"[UsableDeckManager] 총 {usableDeck.Count}장의 카드로 덱을 초기화했습니다.");
    }

}
