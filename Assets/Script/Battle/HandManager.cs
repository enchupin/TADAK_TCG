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
    private List<Card> handCardList = new List<Card>(); // 손패를 Card 객체로 관리

    // never using
    // [SerializeField] private float cardSpacing = 150f;


    /// <summary>
    /// 손패에 카드 한 장 추가
    /// </summary>

    public void AddCard(Card card) {
        if (cardUIPrefab == null || handContainer == null) {
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }
        handCardList.Add(card);
        InstantiateCardUI(card);
    }


    /// <summary>
    /// 손패에 카드 추가
    /// </summary>

    public void AddCard(List<Card> cards)
    {
        if (cardUIPrefab == null || handContainer == null) { // 예외 처리
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }

        // 카드 추가
        foreach (Card card in cards) { 
            handCardList.Add(card);
            InstantiateCardUI(card);
        }
    }



    /// <summary>
    /// 카드 UI 생성
    /// </summary>
    private void InstantiateCardUI(Card card) {
        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);

        // CardController를 통해 초기화
        CardController controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            controller.Initialize(card);
        } else {
            Debug.LogWarning($"[HandManager] CardController를 찾을 수 없습니다!");
        }
    }


    /// <summary>
    /// CadrInteractionHandler를 제외한 카드 추가
    /// </summary>
    public void AddCardWithoutInputController(List<Card> cards) {
        if (cardUIPrefab == null || handContainer == null) { // 예외 처리
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }

        // 카드 추가
        foreach (Card card in cards) {
            handCardList.Add(card);
            InstantiateCardUIWithoutInputController(card);
        }
    }

    /// <summary>
    /// CadrInteractionHandler를 제외한 카드 UI 생성
    /// </summary>
    private void InstantiateCardUIWithoutInputController(Card card) {
        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);

        // CardController를 통해 초기화
        CardController controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            controller.Initialize(card);
            controller.useInteractionHandler = false;
        } else {
            Debug.LogWarning($"[HandManager] CardController를 찾을 수 없습니다!");
        }
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

        // CardController를 통해 cardId 가져오기
        CardController controller = cardUI.GetComponent<CardController>();
        if (controller == null || controller.Card == null)
        {
            Debug.LogWarning("[HandManager] CardController 또는 Card를 찾을 수 없습니다!");
            Destroy(cardUI.gameObject);
            return;
        }
        
        Card card = controller.Card;
        
        // 손패 리스트에서 제거
        if (handCardList.Contains(card))
        {
            handCardList.Remove(card);
            Debug.Log($"[HandManager] 손패에서 카드 {card.cardName} 제거");
        }

        // UI 오브젝트 파괴
        Destroy(cardUI.gameObject);
    }

    

    
    /// <summary>
    /// 손패 비우기 (턴 종료 시)
    /// </summary>
    public List<Card> ClearHand()
    {
        List<Card> discardedCards = new List<Card>(handCardList);
        handCardList.Clear();

        // 손패 UI 모두 파괴
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }

        return discardedCards;
    }
    

    
    /// <summary>
    /// 손패 카드 수 반환
    /// </summary>
    public int GetCardCount()
    {
        return handCardList.Count;
    }
    
    /// <summary>
    /// 손패의 카드 목록 반환
    /// </summary>
    public List<Card> GetHandCards()
    {
        return new List<Card>(handCardList);
    }




}
