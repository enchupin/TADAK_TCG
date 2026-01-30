using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드의 모든 동작을 관리하는 메인 컨트롤러
/// CardUI, CardInteractionHandler를 조율하고 Card 데이터를 관리
/// </summary>
[RequireComponent(typeof(CardUI))]
public class CardController : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] public CardUI cardUI;
    [SerializeField] public CardInteractionHandler interactionHandler;

    public bool isPlayable = false;
    public bool useInteractionHandler = true;


    private Card card;
    public Card Card => card;
    
    private void Start()
    {
        // InteractionHandler 설정
        if (interactionHandler != null)
        {
            interactionHandler.showPlayThreshold = useInteractionHandler;
            interactionHandler.enabled = useInteractionHandler;
            if (useInteractionHandler) {
                // CardInteractionHandler의 카드 플레이 요청 이벤트 구독
                interactionHandler.OnCardPlayRequested.AddListener(HandleCardPlayRequest);
            }
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (interactionHandler != null) {
            interactionHandler.OnCardPlayRequested.RemoveListener(HandleCardPlayRequest);
        }
    }
    
    /// <summary>
    /// 카드 초기화
    /// </summary>
    /// <summary>
    /// 카드 초기화
    /// </summary>
    public void Initialize(Card card)
    {
        if (card == null) {
            Debug.LogError($"[CardController] Card is null!");
            return;
        }
        
        this.card = card;
        cardUI.UpdateDisplay(card);
    }
    
    /// <summary>
    /// CardInteractionHandler로부터 카드 플레이 요청을 받았을 때 처리
    /// </summary>
    private void HandleCardPlayRequest()
    {
        if (card == null) {
            Debug.LogWarning("[CardController] Card is null, cannot play card");
            return;
        }
        // CardController가 직접 이벤트 발행
        CardPlayEventData eventData = new CardPlayEventData(this);
        CardPlayEvents.RaiseCardPlayed(eventData);
    }
}
