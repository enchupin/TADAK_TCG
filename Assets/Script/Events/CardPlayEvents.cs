using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 카드 게임 이벤트 시스템
/// UI와 게임 로직을 분리하기 위한 이벤트 정의
/// </summary>
public static class CardPlayEvents
{
    /// <summary>
    /// 카드가 사용되었을 때 발생하는 이벤트
    /// </summary>
    public static event System.Action<CardPlayEventData> OnCardPlayed;

    /// <summary>
    /// 카드 사용 이벤트 발행
    /// </summary>
    public static void RaiseCardPlayed(CardPlayEventData data)
    {
        OnCardPlayed?.Invoke(data);
    }



}

/// <summary>
/// 카드 클릭 이벤트 데이터
/// </summary>
public class CardPlayEventData
{
    public CardUI cardUI; // 클릭된 카드 UI (UI 업데이트용)
    public Card card;
    public int cardId;
    
    // CardController를 받는 생성자
    public CardPlayEventData(CardController cardController)
    {
        this.cardUI = cardController.cardUI;
        this.card = cardController.Card;
        this.cardId = cardController.Card.cardId;
    }
    
    // 하위 호환성을 위한 CardUI 생성자 (deprecated)
    [System.Obsolete("Use CardClickedEventData(CardController) instead")]
    public CardPlayEventData(CardUI cardUI)
    {
        this.cardUI = cardUI;
        // CardController를 통해 Card 가져오기
        var controller = cardUI.GetComponent<CardController>();
        if (controller != null)
        {
            this.card = controller.Card;
            this.cardId = controller.Card.cardId;
        }
    }
}
