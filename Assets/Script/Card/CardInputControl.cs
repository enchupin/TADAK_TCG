using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 입력(드래그, 클릭)을 처리하는 컨트롤러
/// CardUI에서 로직을 분리하여 드래그 앤 드롭 기능을 구현함
/// </summary>
public class CardInputControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private CardUI cardUI;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;

    private int originalSiblingIndex;
    private bool isDragging = false;

    // 카드를 발동시킬 Y축 임계값 (화면 높이 비율)
    private const float PLAY_THRESHOLD_Y_RATIO = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showPlayThreshold = true;
    [SerializeField] private Color thresholdColor = new Color(1, 0, 0, 0.5f);
    private static GameObject debugLineObject;

    private void Awake()
    {
        cardUI = GetComponent<CardUI>();
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // 드래그 중 레이캐스트 차단을 위한 CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // LayoutGroup의 영향을 받지 않기 위한 LayoutElement
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    private void Start()
    {
        if (showPlayThreshold && debugLineObject == null && canvas != null)
        {
            CreateDebugThresholdLine();
        }
    }




    // 테스트 전용 메서드 (카드 발동 임계 포지션 시각화)
    private void CreateDebugThresholdLine()
    {
        // 디버그 라인 오브젝트 생성
        debugLineObject = new GameObject("Debug_ThresholdLine");
        debugLineObject.transform.SetParent(canvas.transform, false);
        debugLineObject.transform.SetAsLastSibling(); // 맨 위에 그리기

        // 이미지 컴포넌트 추가
        Image img = debugLineObject.AddComponent<Image>();
        img.color = thresholdColor;
        img.raycastTarget = false; // 입력 차단하지 않음

        // 위치 설정 (앵커를 이용해 비율 위치 고정)
        RectTransform rt = debugLineObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, PLAY_THRESHOLD_Y_RATIO);
        rt.anchorMax = new Vector2(1, PLAY_THRESHOLD_Y_RATIO);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 4); // 선 두께
        rt.anchoredPosition = Vector2.zero;
        
        Debug.Log($"[CardInputControl] Debug Line Created at Y-Ratio: {PLAY_THRESHOLD_Y_RATIO}");
    }



    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 시에는 발동하지 않음 (필요 시 확대/상세보기 로직 추가)
        if (!isDragging && cardUI != null && cardUI.card != null)
        {
            Debug.Log($"[CardInputControl] {cardUI.card.cardName} 클릭됨 (발동 안함)");
        }
    }



    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardUI == null || !cardUI.IsPlayable) return;

        isDragging = true;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 레이아웃 무시 (자유로운 이동)
        layoutElement.ignoreLayout = true;
        
        // 레이캐스트 차단 해제 (드롭 위치 감지 등을 위해)
        canvasGroup.blocksRaycasts = false;
        
        // 렌더링 순서 최상위로
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 캔버스 스케일을 고려한 이동
        if (canvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
        else
        {
            rectTransform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;

        // 핸드 영역(화면 하단)을 벗어났는지 체크
        if (eventData.position.y > Screen.height * PLAY_THRESHOLD_Y_RATIO)
        {
            TryPlayCard();
        }
        else
        {
            ReturnToHand();
        }
    }

    private void TryPlayCard()
    {
        Debug.Log($"[CardInputControl] {cardUI.card.cardName} 드래그 발동 시도");
        
        // 이벤트 발행하여 카드 사용 처리
        CardClickedEventData cardClickData = new CardClickedEventData(cardUI);
        CardGameEvents.RaiseCardClicked(cardClickData);
        
        // 시각적으로는 일단 원래 자리로 복귀시킴 (사용 성공 시 HandManager에서 제거될 것임)
        ReturnToHand();
    }

    private void ReturnToHand()
    {
        // 원래 렌더링 순서 복원
        transform.SetSiblingIndex(originalSiblingIndex);
        
        // 위치는 LayoutGroup에 의해 다음 프레임에 자동 정렬됨 (anchoredPosition 초기화는 선택사항)
        rectTransform.anchoredPosition = Vector2.zero; // 간단한 리셋
    }
}
