using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 카드의 모든 상호작용을 담당하는 통합 핸들러
/// 호버 효과, 드래그, 플레이스홀더, 카드 사용 판정
/// </summary>
public class CardInteractionHandler : UIHoverEffect,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Events")]
    [SerializeField] private UnityEvent onCardPlayRequested; // 카드 사용 이벤트
    
    [Header("Drag Settings")]
    private readonly float playThresholdYRatio = 0.3f; // 드래그 범위
    private static bool isAnyCardDragging = false;
    private bool isDragging = false;

    [Header("Debug")]
    public bool showPlayThreshold = true;
    private Color thresholdColor = new Color(1, 0, 0, 0.5f);
    private static GameObject debugLineObject;

    [Header("Hover Settings")]
    private readonly float cardHoverScale = 1.4f;
    private readonly float cardHoverDuration = 0.15f;

    [Header("Drag Component")]
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private CardUI cardUI; // CardController 대신 CardUI 직접 참조
    
    private int originalSiblingIndex;
    private GameObject placeholder;
    public UnityEvent OnCardPlayRequested => onCardPlayRequested; // 카드 사용 이벤트


    protected override void Awake()
    {
        base.Awake(); // UIHoverEffect initialization
        
        // 호버링 설정
        SetHoverScale(cardHoverScale);
        SetAnimationDuration(cardHoverDuration);
        
        // 드래그 초기화
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        cardUI = GetComponent<CardUI>(); // 추후 CardUI를 직접 참조하지 않는 방식으로 변경 예정
        
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
    }
    
    
    private void Start()
    {
        // 모든 컴포넌트의 Start()가 완료된 후에 체크하도록 지연
        Invoke(nameof(CheckAndCreateThresholdLine), 0.01f);
    }

    private void CheckAndCreateThresholdLine()
    {
        if (showPlayThreshold && debugLineObject == null && canvas != null)
        {
            CreateDebugThresholdLine();
        }
    }
    
    #region Hover Effects
    
    /// <summary>
    /// 호버링 적용
    /// </summary>
    public override void OnPointerEnter(PointerEventData eventData)
    {
        // 어떤 카드라도 드래그 중이면 호버 효과를 무시
        if (!isDragging && !isAnyCardDragging) {
            base.OnPointerEnter(eventData);
        }
    }
    
    /// <summary>
    /// 호버링 해제
    /// </summary>
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }
    
    #endregion
    
    #region Drag & Play
    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardUI == null || !cardUI.IsPlayable) return;
        
        isDragging = true;
        isAnyCardDragging = true; // 전역 드래그 상태 활성화
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Placeholder 생성 전에 크기 초기화
        StopAnimation();
        transform.localScale = originalScale;
        
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
        if (canvas == null) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            canvas.worldCamera,
            out Vector2 localPoint);
            
        rectTransform.position = canvas.transform.TransformPoint(localPoint);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        isAnyCardDragging = false; // 전역 드래그 상태 비활성화
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;
        
        // 플레이스홀더 제거
        DestroyPlaceholder();
        
        // 카드 사용 판정
        if (eventData.position.y > Screen.height * playThresholdYRatio) {
            // UnityEvent 발행
            onCardPlayRequested?.Invoke();
        }
        else {
            ReturnToHand();
        }
        
        // 원래 크기로 복원
        StopAnimation();
        StartCoroutine(AnimateScale(originalScale));
    }
    
    
    private void ReturnToHand()
    {
        transform.SetSiblingIndex(originalSiblingIndex);
        rectTransform.anchoredPosition = Vector2.zero;
    }
    
    #endregion
    
    #region Placeholder
    
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
    
    private void DestroyPlaceholder()
    {
        if (placeholder != null)
        {
            Destroy(placeholder);
            placeholder = null;
        }
    }
    
    #endregion
    
    #region Debug
    
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
    
    #endregion
    
}
