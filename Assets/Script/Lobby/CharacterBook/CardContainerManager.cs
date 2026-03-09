using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 카드 컨테이너 관리 시스템
/// 카드 ID를 정수로 관리하며, UI 생성/제거, 레이아웃 관리
/// </summary>
public class CardContainerManager : MonoBehaviour {
    [Header("프리팹")]
    [SerializeField] private GameObject cardUIPrefab;

    [Header("레이아웃")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Transform cardContainerScrollView;


    [Header("Grid 레이아웃 설정")]
    private Vector2 cellSize = new Vector2(200, 280); // 각 셀의 크기
    private Vector2 spacing = new Vector2(10, 10); // 카드 간 간격
    private int paddingLeft = 5;
    private int paddingRight = 5;
    private int paddingTop = 10;
    private int paddingBottom = 10;

    [Header("스크롤 설정")]
    [Range(1f, 100f)] private float scrollSensitivity = 7.5f; // 스크롤 휠 감도

    [Header("카드 설정")]
    [Range(0.1f, 2f)] private float cardScale = 0.65f; // 카드 크기 조절

    [Header("카드 컨테이너")]
    private List<Card> cardList = new List<Card>(); // 카드 컨테이너를 Card 객체로 관리
    
    private GridLayoutGroup gridLayout;
    private Action<CardController> onCardClicked;
    

    private void Awake() {
        SetupGridLayout();
    }

    private void Start() {
        // RectTransform이 완전히 초기화된 후 열 개수 재계산
        CalculateColumnCount();
        
        // 스크롤 감도 설정
        SetupScrollSensitivity();
    }

    /// <summary>
    /// 컨테이너 너비에 맞게 열 개수 자동 계산
    /// </summary>
    private void CalculateColumnCount() {
        if (cardContainer == null || gridLayout == null) return;

        RectTransform rectTransform = cardContainerScrollView.GetComponent<RectTransform>();
        if (rectTransform == null) {
            Debug.LogError("[CardContainerManager] cardContainer에 RectTransform이 없습니다!");
            return;
        }

        // 사용 가능한 너비 계산
        float availableWidth = rectTransform.rect.width - paddingLeft - paddingRight;
        
        // 한 장의 카드가 차지하는 너비 (셀 크기 + 간격)
        float cardWidthWithSpacing = cellSize.x + spacing.x;
        
        // 한 줄에 들어갈 수 있는 최대 카드 개수 계산
        // 마지막 카드 뒤에는 spacing이 필요 없으므로 spacing.x를 더해줌
        int calculatedColumnCount = Mathf.FloorToInt((availableWidth + spacing.x) / cardWidthWithSpacing);
        
        // 최소 1개는 보장
        calculatedColumnCount = Mathf.Max(1, calculatedColumnCount);
        
        gridLayout.constraintCount = calculatedColumnCount;
        
        Debug.Log($"[CardContainerManager] 자동 계산된 열 개수: {calculatedColumnCount} (컨테이너 너비: {rectTransform.rect.width}, 사용가능 너비: {availableWidth})");
    }

    /// <summary>
    /// 스크롤뷰의 휠 감도 설정
    /// </summary>
    private void SetupScrollSensitivity() {
        if (cardContainerScrollView == null) {
            Debug.LogWarning("[CardContainerManager] cardContainerScrollView가 설정되지 않았습니다!");
            return;
        }

        // ScrollRect 컴포넌트 찾기
        ScrollRect scrollRect = cardContainerScrollView.GetComponent<ScrollRect>();
        if (scrollRect == null) {
            Debug.LogWarning("[CardContainerManager] ScrollRect 컴포넌트를 찾을 수 없습니다!");
            return;
        }

        // 스크롤 감도 설정
        scrollRect.scrollSensitivity = scrollSensitivity;
        Debug.Log($"[CardContainerManager] 스크롤 감도 설정: {scrollSensitivity}");
    }

    /// <summary>
    /// Grid Layout Group 설정
    /// </summary>
    private void SetupGridLayout() {
        if (cardContainer == null) {
            Debug.LogError("[CardContainerManager] cardContainer가 설정되지 않았습니다!");
            return;
        }

        // Grid Layout Group 컴포넌트 가져오기 또는 추가
        gridLayout = cardContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null) {
            gridLayout = cardContainer.gameObject.AddComponent<GridLayoutGroup>();
            Debug.Log("[CardContainerManager] GridLayoutGroup 컴포넌트를 추가했습니다.");
        }

        // Grid Layout 설정
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft; // 좌측 상단부터 시작
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal; // 가로 방향으로 채움
        gridLayout.childAlignment = TextAnchor.UpperLeft; // 좌측 상단 정렬
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 열 개수 고정
        gridLayout.cellSize = cellSize; // 셀 크기
        gridLayout.spacing = spacing; // 간격
        gridLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom); // 여백

        Debug.Log($"[CardContainerManager] Grid Layout 설정 완료 (셀 크기: {cellSize}, 간격: {spacing})");

        // Content Size Fitter 추가 (스크롤을 위해 필요)
        ContentSizeFitter sizeFitter = cardContainer.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null) {
            sizeFitter = cardContainer.gameObject.AddComponent<ContentSizeFitter>();
            Debug.Log("[CardContainerManager] ContentSizeFitter 컴포넌트를 추가했습니다.");
        }
        
        // 세로 방향으로만 크기 자동 조절 (스크롤 가능하도록)
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }


    /// <summary>
    /// 카드 컨테이너에 카드 한 장 추가
    /// </summary>

    public void AddCard(Card card) {
        if (cardUIPrefab == null || cardContainer == null) {
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }
        cardList.Add(card);
        InstantiateCardUI(card);
    }


    /// <summary>
    /// 카드 컨테이너에 카드 추가
    /// </summary>

    public void AddCard(List<Card> cards) {
        if (cardUIPrefab == null || cardContainer == null) { // 예외 처리
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }

        // 카드 추가
        foreach (Card card in cards) {
            cardList.Add(card);
            InstantiateCardUI(card);
        }
    }



    /// <summary>
    /// 카드 UI 생성
    /// </summary>
    private void InstantiateCardUI(Card card) {
        GameObject cardObj = Instantiate(cardUIPrefab, cardContainer);
        
        // 카드 크기 조정
        cardObj.transform.localScale = Vector3.one * cardScale;

        // CardController를 통해 초기화
        CardController controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            controller.Initialize(card);
            BindCardClickHandler(cardObj, controller);
        } else {
            Debug.LogWarning($"[CardContainerManager] CardController를 찾을 수 없습니다!");
        }
    }


    /// <summary>
    /// CadrInteractionHandler를 제외한 카드 추가
    /// </summary>
    public void AddCardWithoutInputController(List<Card> cards) {
        Debug.Log($"[CardContainerManager] AddCardWithoutInputController 호출 - 카드 개수: {cards.Count}");
        Debug.Log($"[CardContainerManager] cardUIPrefab: {(cardUIPrefab != null ? "설정됨" : "NULL")}");
        Debug.Log($"[CardContainerManager] handContainer: {(cardContainer != null ? "설정됨" : "NULL")}");
        
        if (cardUIPrefab == null || cardContainer == null) { // 예외 처리
            Debug.LogError("CardUI 프리팹 또는 Hand Container가 설정되지 않았습니다!");
            return;
        }

        // 카드 추가
        int addedCount = 0;
        foreach (Card card in cards) {
            cardList.Add(card);
            InstantiateCardUIWithoutInputController(card);
            addedCount++;
            Debug.Log($"[CardContainerManager] 카드 UI 생성: {card.cardName} ({addedCount}/{cards.Count})");
        }
        
        Debug.Log($"[CardContainerManager] 총 {addedCount}개의 카드 UI가 생성되었습니다.");
    }

    /// <summary>
    /// CadrInteractionHandler를 제외한 카드 UI 생성
    /// </summary>
    private void InstantiateCardUIWithoutInputController(Card card) {
        Debug.Log($"[CardContainerManager] InstantiateCardUIWithoutInputController 시작 - 카드: {card.cardName}");
        
        GameObject cardObj = Instantiate(cardUIPrefab, cardContainer);
        Debug.Log($"[CardContainerManager] 카드 오브젝트 생성 완료: {cardObj.name}");
        
        // 카드 크기 조정
        cardObj.transform.localScale = Vector3.one * cardScale;
        Debug.Log($"[CardContainerManager] 카드 크기 조정: {cardScale}");

        // CardController를 통해 초기화
        CardController controller = cardObj.GetComponent<CardController>();
        if (controller != null) {
            Debug.Log($"[CardContainerManager] CardController 발견, 초기화 중...");
            controller.Initialize(card);
            controller.useInteractionHandler = false;
            BindCardClickHandler(cardObj, controller);
            Debug.Log($"[CardContainerManager] CardController 초기화 완료 (useInteractionHandler=false)");
        } else {
            Debug.LogWarning($"[CardContainerManager] CardController를 찾을 수 없습니다! 프리팹에 CardController가 있는지 확인하세요.");
        }
    }

    /// <summary>
    /// 카드 컨테이너에서 특정 카드 제거 (카드 사용 시)
    /// </summary>
    public void RemoveCardFromHand(CardUI cardUI) {
        if (cardUI == null) {
            Debug.LogWarning("[CardContainerManager] CardUI가 null입니다!");
            return;
        }

        // CardController를 통해 cardId 가져오기
        CardController controller = cardUI.GetComponent<CardController>();
        if (controller == null || controller.Card == null) {
            Debug.LogWarning("[CardContainerManager] CardController 또는 Card를 찾을 수 없습니다!");
            Destroy(cardUI.gameObject);
            return;
        }

        Card card = controller.Card;

        // 카드 컨테이너 리스트에서 제거
        if (cardList.Contains(card)) {
            cardList.Remove(card);
            Debug.Log($"[CardContainerManager] 카드 컨테이너에서 카드 {card.cardName} 제거");
        }

        // UI 오브젝트 파괴
        Destroy(cardUI.gameObject);
    }




    /// <summary>
    /// 카드 컨테이너 비우기 (턴 종료 시)
    /// </summary>
    public List<Card> ClearHand() {
        List<Card> discardedCards = new List<Card>(cardList);
        cardList.Clear();

        // 카드 컨테이너 UI 모두 파괴
        foreach (Transform child in cardContainer) {
            Destroy(child.gameObject);
        }

        return discardedCards;
    }



    /// <summary>
    /// 카드 컨테이너 카드 수 반환
    /// </summary>
    public int GetCardCount() {
        return cardList.Count;
    }

    /// <summary>
    /// 카드 컨테이너의 카드 목록 반환
    /// </summary>
    public List<Card> GetHandCards() {
        return new List<Card>(cardList);
    }

    public void SetCardClickHandler(Action<CardController> clickHandler) {
        onCardClicked = clickHandler;
    }

    private void BindCardClickHandler(GameObject cardObj, CardController controller) {
        if (cardObj == null || controller == null || onCardClicked == null) {
            return;
        }

        CardSelectionClickHandler clickHandler = cardObj.GetComponent<CardSelectionClickHandler>();
        if (clickHandler == null) {
            clickHandler = cardObj.AddComponent<CardSelectionClickHandler>();
        }

        clickHandler.Bind(controller, () => onCardClicked?.Invoke(controller));
    }




}

public class CardSelectionClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CardController cardController;
    private Action onClicked;

    public void Bind(CardController controller, Action clickAction) {
        cardController = controller;
        onClicked = clickAction;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left) {
            return;
        }

        if (cardController == null) {
            return;
        }

        onClicked?.Invoke();
    }
}
