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
    // 관리하는 컴포넌트들
    [Header("컴포넌트")]
    [SerializeField] private CardUI cardUI;
    [SerializeField] private CardInteractionHandler interactionHandler;
    

    private Card card;
    public Card Card => card;
    
    /// <summary>
    /// 카드 초기화
    /// </summary>
    public void Initialize(int cardId)
    {

        // 카드 초기화하는 메서드
        card = new Card();


        // CardUI에 Card 데이터 전달
        cardUI.UpdateDisplay(card);
    }
    
    
    // 각 컴포넌트에 대한 접근자
    public CardUI UI => cardUI;
    public CardInteractionHandler InteractionHandler => interactionHandler;
}
