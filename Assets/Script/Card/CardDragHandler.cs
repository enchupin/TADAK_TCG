using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 드래그 및 사용 판정을 담당하는 핸들러
/// 드래그, 플레이스홀더, 카드 사용 임계값 체크
/// </summary>
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Play Threshold")]
    [SerializeField] private float playThresholdYRatio = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool showPlayThreshold = true;
    [SerializeField] private Color thresholdColor = new Color(1, 0, 0, 0.5f);
    private static GameObject debugLineObject;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    
    private CardHoverHandler hoverHandler;
    private CardController cardController;
    
    private int originalSiblingIndex;
    private GameObject placeholder;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        hoverHandler = GetComponent<CardHoverHandler>();
        cardController = GetComponent<CardController>();
        
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
    }
    
    private void Start()
    {
        if (showPlayThreshold && debugLineObject == null && canvas != null)
        {
            CreateDebugThresholdLine();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardController == null || cardController.UI == null || !cardController.UI.IsPlayable) return;
        
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // 호버 핸들러에 드래그 시작 알림
        hoverHandler?.OnDragStart();
        
        // Placeholder 생성 전에 크기 초기화
        hoverHandler?.ResetScaleImmediate();
        
        // Placeholder 생성
        CreatePlaceholder();
        
        // 레이아웃 무시
        layoutElement.ignoreLayout = true;
        canvasGroup.blocksRaycasts = false;
        
        // 렌더링 순서 최상위로
        transform.SetAsLastSibling();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
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
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;
        
        // 플레이스홀더 제거
        DestroyPlaceholder();
        
        // 호버 핸들러에 드래그 종료 알림
        hoverHandler?.OnDragEnd();
        
        // 카드 사용 판정
        if (eventData.position.y > Screen.height * playThresholdYRatio)
        {
            TryPlayCard();
        }
        else
        {
            ReturnToHand();
        }
    }
    
    /// <summary>
    /// 카드 사용 시도
    /// </summary>
    private void TryPlayCard()
    {
        if (cardController == null || cardController.Card == null) return;
        
        Debug.Log($"[CardDragHandler] {cardController.Card.cardName} 드래그 발동 시도");
        
        // 이벤트 발행 - CardController 전달
        CardClickedEventData cardClickData = new CardClickedEventData(cardController);
        CardGameEvents.RaiseCardClicked(cardClickData);
        
        // 원래 자리로 복귀
        ReturnToHand();
    }
    
    /// <summary>
    /// 손으로 복귀
    /// </summary>
    private void ReturnToHand()
    {
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = Vector2.zero;
    }
    
    /// <summary>
    /// Placeholder 생성
    /// </summary>
    private void CreatePlaceholder()
    {
        if (placeholder != null) return;
        
        placeholder = new GameObject("CardPlaceholder");
        placeholder.transform.SetParent(transform.parent, false);
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);
        
        var placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = rectTransform.sizeDelta;
        
        var placeholderLayout = placeholder.AddComponent<LayoutElement>();
        
        if (layoutElement != null)
        {
            placeholderLayout.minWidth = layoutElement.minWidth;
            placeholderLayout.minHeight = layoutElement.minHeight;
            placeholderLayout.preferredWidth = layoutElement.preferredWidth;
            placeholderLayout.preferredHeight = layoutElement.preferredHeight;
            placeholderLayout.flexibleWidth = layoutElement.flexibleWidth;
            placeholderLayout.flexibleHeight = layoutElement.flexibleHeight;
            placeholderLayout.layoutPriority = layoutElement.layoutPriority;
        }
        else
        {
            placeholderLayout.preferredWidth = rectTransform.rect.width;
            placeholderLayout.preferredHeight = rectTransform.rect.height;
        }
    }
    
    /// <summary>
    /// Placeholder 제거
    /// </summary>
    private void DestroyPlaceholder()
    {
        if (placeholder != null)
        {
            Destroy(placeholder);
            placeholder = null;
        }
    }
    
    /// <summary>
    /// 디버그 임계선 생성
    /// </summary>
    private void CreateDebugThresholdLine()
    {
        debugLineObject = new GameObject("Debug_ThresholdLine");
        debugLineObject.transform.SetParent(canvas.transform, false);
        debugLineObject.transform.SetAsLastSibling();
        
        var img = debugLineObject.AddComponent<Image>();
        img.color = thresholdColor;
        img.raycastTarget = false;
        
        var rt = debugLineObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, playThresholdYRatio);
        rt.anchorMax = new Vector2(1, playThresholdYRatio);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 4);
        rt.anchoredPosition = Vector2.zero;
    }
}
