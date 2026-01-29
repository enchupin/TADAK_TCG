using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 카드의 모든 동작을 관리하는 메인 컨트롤러
/// CardUI, CardInteractionHandler를 조율하고 Card 데이터를 관리
/// </summary>
[RequireComponent(typeof(CardUI))]
[RequireComponent(typeof(CardInteractionHandler))]
public class CardController : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private CardUI cardUI;
    [SerializeField] private CardInteractionHandler interactionHandler;
    

    private Card card;


    private CardData cardData;
    public Card Card => card;
    
    /// <summary>
    /// 카드 초기화 - cardId를 기반으로 Card 객체를 로드
    /// </summary>
    public void Initialize(int cardId)
    {
        // CardManager에서 Card를 직접 로드
        card = CardManager.GetCardAsCard(cardId);
        if (card == null) {
            Debug.LogError($"[CardController] Card with ID {cardId} not found!");
            return;
        }
        cardUI.UpdateDisplay(card);
    }
    
    
    // 각 컴포넌트에 대한 접근자
    public CardUI UI => cardUI;
    public CardInteractionHandler InteractionHandler => interactionHandler;
}
