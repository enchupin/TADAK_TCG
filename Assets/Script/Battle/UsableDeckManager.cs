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
    /// 외부에서 덱을 설정 (전투 시작 시 호출)
    /// </summary>
    public void SetDeck(List<int> cardIds) {
        usableDeck = new Queue<int>();
        
        foreach (int id in cardIds) {
            usableDeck.Enqueue(id);
        }
        
        Debug.Log($"[UsableDeckManager] 덱 설정 완료: 총 {usableDeck.Count}장");
    }

}
