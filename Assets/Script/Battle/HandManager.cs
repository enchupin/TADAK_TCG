using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 손패 관리 시스템
/// 카드 ID를 정수로 관리하며, UI 생성/제거, 레이아웃 관리
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private GameObject cardUIPrefab;
    
    [Header("레이아웃")]
    [SerializeField] private Transform handContainer;

    [Header("손패")]
    private List<int> handCardIdList = new List<int>(); // 손패를 카드 ID로 관리

    // never using
    // [SerializeField] private float cardSpacing = 150f;


    /// <summary>
    /// 손패에 카드 한 장 추가
    /// </summary>

    public void AddCardById(int cardId) {
        if (cardUIPrefab == null || handContainer == null) {
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }
        handCardIdList.Add(cardId);
        InstantiateCardUI(cardId);
    }


    /// <summary>
    /// 손패에 카드 추가
    /// </summary>

    public void AddCardById(List<int> cardIds)
    {
        if (cardUIPrefab == null || handContainer == null) { // 예외 처리
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }

        // 카드 추가
        foreach (int cardId in cardIds) { 
            handCardIdList.Add(cardId);
            InstantiateCardUI(cardId);
        }
        
        UpdateLayout();
    }


    /// <summary>
    /// 카드 UI 생성
    /// </summary>
    public void InstantiateCardUI(int cardId) {
        Card card = CardDatabase.Instance?.GetCardById(cardId);
        if (card == null) {
            Debug.LogWarning($"카드 ID {cardId}를 찾을 수 없습니다!");
            return;
        }

        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        cardUI.InitializeCardUI(cardId);


    }


    /// <summary>
    /// 손패에서 특정 카드 제거 (카드 사용 시)
    /// </summary>
    public void RemoveCardFromHand(CardUI cardUI)
    {
        if (cardUI == null)
        {
            Debug.LogWarning("[HandManager] CardUI가 null입니다!");
            return;
        }

        int cardId = cardUI.card.cardId;
        
        // 손패 리스트에서 제거
        if (handCardIdList.Contains(cardId))
        {
            handCardIdList.Remove(cardId);
            Debug.Log($"[HandManager] 손패에서 카드 ID {cardId} 제거");
        }

        // UI 오브젝트 파괴
        Destroy(cardUI.gameObject);
        
        UpdateLayout();
    }

    


    
    /// <summary>
    /// 손패 비우기 (턴 종료 시)
    /// </summary>
    public List<int> ClearHand()
    {
        List<int> discardedCards = new List<int>(handCardIdList);
        handCardIdList.Clear();

        // 손패 UI 모두 파괴
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }

        return discardedCards;
    }
    


    /// <summary>
    /// 레이아웃 업데이트
    /// </summary>
    private void UpdateLayout()
    {
        // Horizontal Layout Group이 자동으로 처리
        // 필요시 수동 배치 로직 추가 가능
    }
    
    /// <summary>
    /// 손패 카드 수 반환
    /// </summary>
    public int GetCardCount()
    {
        return handCardIdList.Count;
    }
    
    /// <summary>
    /// 손패의 카드 ID 목록 반환
    /// </summary>
    public List<int> GetHandCardIds()
    {
        return new List<int>(handCardIdList);
    }




}
