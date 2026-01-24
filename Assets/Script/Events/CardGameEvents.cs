using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 카드 게임 이벤트 시스템
/// UI와 게임 로직을 분리하기 위한 이벤트 정의
/// </summary>
public static class CardGameEvents
{
    /// <summary>
    /// 카드가 클릭되었을 때 발생하는 이벤트
    /// </summary>
    public static event System.Action<CardClickedEventData> OnCardClicked;

    /// <summary>
    /// 카드 클릭 이벤트 발행
    /// </summary>
    public static void RaiseCardClicked(CardClickedEventData data)
    {
        OnCardClicked?.Invoke(data);
    }
}

/// <summary>
/// 카드 클릭 이벤트 데이터
/// </summary>
public class CardClickedEventData
{
    public CardUI cardUI; // 클릭된 카드 UI (UI 업데이트용)
    public Card card;
    public int cardId;
    
    public CardClickedEventData(CardUI cardUI)
    {
        this.cardUI = cardUI;
        this.card = cardUI.card;
        this.cardId = cardUI.card.cardId;
    }
}
