using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드의 모든 동작을 관리하는 메인 컨트롤러
/// CardUI, CardHoverHandler, CardDragHandler를 조율하고 Card 데이터를 관리
/// </summary>
[RequireComponent(typeof(CardUI))]
[RequireComponent(typeof(CardHoverHandler))]
[RequireComponent(typeof(CardDragHandler))]
public class CardController : MonoBehaviour, IPointerClickHandler
{
    // 관리하는 컴포넌트들
    private CardUI cardUI;
    private CardHoverHandler hoverHandler;
    private CardDragHandler dragHandler;
    
    // Card 데이터 (CardController가 소유)
    private Card card;
    public Card Card => card;
    
    private void Awake()
    {
        // 컴포넌트 가져오기
        cardUI = GetComponent<CardUI>();
        hoverHandler = GetComponent<CardHoverHandler>();
        dragHandler = GetComponent<CardDragHandler>();
        
        // 없는 경우 자동 추가
        if (cardUI == null) cardUI = gameObject.AddComponent<CardUI>();
        if (hoverHandler == null) hoverHandler = gameObject.AddComponent<CardHoverHandler>();
        if (dragHandler == null) dragHandler = gameObject.AddComponent<CardDragHandler>();
    }
    
    /// <summary>
    /// 카드 초기화 - Card 데이터를 로드하고 UI 업데이트
    /// </summary>
    public void Initialize(int cardId)
    {
        // CardController가 Card를 직접 로드
        card = TrainingBattleManager.Instance.GetCardById(cardId);
        
        if (card == null)
        {
            Debug.LogError($"[CardController] Card with ID {cardId} not found!");
            return;
        }
        
        // CardUI에 Card 데이터 전달
        cardUI?.UpdateDisplay(card);
    }
    
    /// <summary>
    /// 카드 사용 가능 여부 설정
    /// </summary>
    public void SetPlayable(bool playable)
    {
        cardUI?.SetPlayable(playable);
    }
    
    /// <summary>
    /// 클릭 이벤트 처리
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (card != null)
        {
            Debug.Log($"[CardController] {card.cardName} 클릭됨");
        }
    }
    
    // 각 컴포넌트에 대한 접근자
    public CardUI UI => cardUI;
    public CardHoverHandler HoverHandler => hoverHandler;
    public CardDragHandler DragHandler => dragHandler;
}
