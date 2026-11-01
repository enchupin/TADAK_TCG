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
    [Tooltip("일부 카드 중 선택(강화 선택지 등)을 화면 가운데에 놓을 영역 (스크롤 밖, 비우면 기본 격자 사용)")]
    [SerializeField] private RectTransform centeredContainer;
    [Tooltip("가운데 영역의 한 줄 카드 수 (고유 카드 5장이 한 줄에 보이도록)")]
    [SerializeField] private int centeredColumns = 5;


    [Header("Grid 레이아웃 설정")]
    // 기본값은 전체 덱 묶음 화면과 같은 크기 (카드 확인 크기, 이전 고정 값은 간격 10 · 여백 5/10 · 배율 0.65)
    [SerializeField] private Vector2 cellSize = new Vector2(200, 280); // 각 셀의 크기
    [Tooltip("한 줄 카드 수 (화면비 · 패널 폭과 관계없이 고정)")]
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 spacing = new Vector2(16, 16); // 카드 간 간격
    [SerializeField] private int paddingLeft = 16;
    [SerializeField] private int paddingRight = 16;
    [SerializeField] private int paddingTop = 16;
    [SerializeField] private int paddingBottom = 16;

    [Header("스크롤 설정")]
    [SerializeField, Range(1f, 100f)] private float scrollSensitivity = 7.5f; // 스크롤 휠 감도

    [Header("카드 설정")]
    [Tooltip("카드 배율 (카드 확인 화면 크기 = 1), 셀 크기도 함께 맞춰야 겹치지 않음")]
    [SerializeField, Range(0.1f, 2f)] private float cardScale = 1f; // 카드 크기 조절

    [Header("카드 컨테이너")]
    private List<Card> cardList = new List<Card>(); // 카드 컨테이너를 Card 객체로 관리
    
    private GridLayoutGroup gridLayout;
    private bool useCenteredContainer;
    // 카드를 만들 현재 영역 (가운데 영역을 쓰는 동안은 그쪽)
    private Transform ActiveContainer => useCenteredContainer && centeredContainer != null ? centeredContainer : cardContainer;
    private Action<CardController> onCardClicked;
    

    private void Awake() {
        SetupGridLayout();
    }

    private void Start() {
        // RectTransform이 완전히 초기화된 후 열 배치 적용
        ApplyColumnLayout();
        
        // 스크롤 감도 설정
        SetupScrollSensitivity();
    }

    /// <summary>
    /// 정해 둔 열 수로 배치, 남는 폭은 좌우 여백으로 나눠 격자 묶음을 가운데에 둠
    /// 카드가 한 줄보다 적어도 열 위치가 같고 왼쪽부터 채움
    /// </summary>
    private void ApplyColumnLayout() {
        if (cardContainer == null || gridLayout == null) return;

        int columnCount = Mathf.Max(1, columns);
        gridLayout.constraintCount = columnCount;
        gridLayout.childAlignment = TextAnchor.UpperLeft;

        // 스크롤바를 뺀 실제 보이는 폭(Viewport) 기준
        RectTransform viewport = cardContainer.parent as RectTransform;
        float gridWidth = columnCount * cellSize.x + (columnCount - 1) * spacing.x;
        float extra = viewport != null ? Mathf.Max(0f, viewport.rect.width - paddingLeft - paddingRight - gridWidth) : 0f;
        int extraLeft = Mathf.FloorToInt(extra * 0.5f);
        gridLayout.padding = new RectOffset(paddingLeft + extraLeft, paddingRight + Mathf.FloorToInt(extra) - extraLeft, paddingTop, paddingBottom);
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
        gridLayout.childAlignment = TextAnchor.UpperLeft; // 좌측 상단 정렬 (남는 폭은 ApplyColumnLayout에서 좌우 여백으로)
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
        GameObject cardObj = Instantiate(cardUIPrefab, ActiveContainer);
        
        // 카드 크기 조정
        cardObj.transform.localScale = Vector3.one * cardScale;
        UIHoverEffect hoverEffect = cardObj.GetComponent<UIHoverEffect>();
        hoverEffect?.CaptureCurrentScaleAsOriginal();

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

        // 패널 크기 · 화면비가 바뀌었을 수 있어 좌우 여백 다시 계산 (열 수는 고정)
        ApplyColumnLayout();
        if (useCenteredContainer) {
            FitCenteredContainer(cards.Count);
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
        
        GameObject cardObj = Instantiate(cardUIPrefab, ActiveContainer);
        Debug.Log($"[CardContainerManager] 카드 오브젝트 생성 완료: {cardObj.name}");
        
        // 카드 크기 조정
        cardObj.transform.localScale = Vector3.one * cardScale;
        UIHoverEffect hoverEffect = cardObj.GetComponent<UIHoverEffect>();
        hoverEffect?.CaptureCurrentScaleAsOriginal();
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
        if (centeredContainer != null) {
            foreach (Transform child in centeredContainer) {
                Destroy(child.gameObject);
            }
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

    // 전투 카드 선택 목록을 다시 그린 뒤 고른 상태를 복원할 때 사용
    public Transform CardContainer => ActiveContainer;

    /// <summary>
    /// 일부 카드 중 선택은 가운데 영역에 같은 크기 · 열 수로 가운데 정렬, 그 외(더미 · 덱 목록)는 스크롤 격자
    /// </summary>
    public void SetCenteredLayout(bool centered) {
        useCenteredContainer = centered && centeredContainer != null;
        if (centeredContainer != null) {
            if (useCenteredContainer) {
                SetupCenteredGrid();
            }
            centeredContainer.gameObject.SetActive(useCenteredContainer);
        }
        if (cardContainer != null) {
            cardContainer.gameObject.SetActive(!useCenteredContainer);
        }
    }

    // 가운데 영역 크기를 카드 수에 맞춤 (확인 버튼을 영역 바로 아래에 붙이기 위해)
    private void FitCenteredContainer(int cardCount) {
        int perRow = Mathf.Max(1, centeredColumns);
        int columnCount = Mathf.Clamp(cardCount, 1, perRow);
        int rowCount = Mathf.Max(1, Mathf.CeilToInt(cardCount / (float)perRow));
        float width = columnCount * cellSize.x + (columnCount - 1) * spacing.x;
        float height = rowCount * cellSize.y + (rowCount - 1) * spacing.y;
        // 씬에서 정한 영역의 중심(Pos 포함)은 그대로 두고 양쪽을 똑같이 줄이고 늘림 (앵커 · 피벗과 무관)
        RectTransform parent = centeredContainer.parent as RectTransform;
        Vector2 parentSize = parent != null ? parent.rect.size : Vector2.zero;
        Vector2 anchorSpan = Vector2.Scale(parentSize, centeredContainer.anchorMax - centeredContainer.anchorMin);
        Vector2 pivotOffset = centeredContainer.pivot - new Vector2(0.5f, 0.5f);
        Vector2 centerOffset = centeredContainer.anchoredPosition - Vector2.Scale(pivotOffset, centeredContainer.sizeDelta);
        Vector2 sizeDelta = new Vector2(width, height) - anchorSpan;
        centeredContainer.sizeDelta = sizeDelta;
        centeredContainer.anchoredPosition = centerOffset + Vector2.Scale(pivotOffset, sizeDelta);
    }

    // 지금 카드가 놓인 영역 (가운데 영역 또는 스크롤 Viewport), 확인 버튼 배치 기준
    public RectTransform VisibleContentArea => useCenteredContainer && centeredContainer != null
        ? centeredContainer
        : cardContainer != null ? cardContainer.parent as RectTransform : null;

    private void SetupCenteredGrid() {
        GridLayoutGroup centeredGrid = centeredContainer.GetComponent<GridLayoutGroup>();
        if (centeredGrid == null) {
            centeredGrid = centeredContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        centeredGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        centeredGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        centeredGrid.childAlignment = TextAnchor.MiddleCenter;
        centeredGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        centeredGrid.constraintCount = Mathf.Max(1, centeredColumns);
        centeredGrid.cellSize = cellSize;
        centeredGrid.spacing = spacing;
        centeredGrid.padding = new RectOffset();
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
