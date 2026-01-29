using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 카드의 모든 상호작용을 담당하는 통합 핸들러
/// 호버 효과, 드래그, 플레이스홀더, 카드 사용 판정
/// </summary>
public class CardInteractionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.4f;
    [SerializeField] private float hoverDuration = 0.15f;
    
    [Header("Drag Settings")]
    [SerializeField] private float playThresholdYRatio = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool showPlayThreshold = true;
    [SerializeField] private Color thresholdColor = new Color(1, 0, 0, 0.5f);
    private static GameObject debugLineObject;
    
    // 호버 관련
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isDragging = false;
    
    // 전역 드래그 상태 (다른 카드들의 호버를 막기 위해)
    private static bool isAnyCardDragging = false;
    
    // 드래그 관련
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private CardController cardController;
    
    private int originalSiblingIndex;
    private GameObject placeholder;
    
    private void Awake()
    {
        // 호버 초기화
        originalScale = transform.localScale;
        
        // 드래그 초기화
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
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
    
    #region Hover Effects
    
    /// <summary>
    /// 마우스가 카드 위로 올라갔을 때
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 어떤 카드라도 드래그 중이면 호버 효과를 무시
        if (!isDragging && !isAnyCardDragging)
        {
            StopCurrentAnimation();
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale * hoverScale));
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            StopCurrentAnimation();
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
        }
    }
    
    private void StopCurrentAnimation()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }
    
    private IEnumerator ScaleAnimation(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hoverDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    #endregion
    
    #region Drag & Play
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardController == null || cardController.UI == null || !cardController.UI.IsPlayable) return;
        
        isDragging = true;
        isAnyCardDragging = true; // 전역 드래그 상태 활성화
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Placeholder 생성 전에 크기 초기화
        StopCurrentAnimation();
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
        isDragging = false;
        isAnyCardDragging = false; // 전역 드래그 상태 비활성화
        canvasGroup.blocksRaycasts = true;
        layoutElement.ignoreLayout = false;
        
        // 플레이스홀더 제거
        DestroyPlaceholder();
        
        // 카드 사용 판정
        if (eventData.position.y > Screen.height * playThresholdYRatio)
        {
            TryPlayCard();
        }
        else
        {
            ReturnToHand();
        }
        
        // 원래 크기로 복원
        StopCurrentAnimation();
        scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
    }
    
    private void TryPlayCard()
    {
        if (cardController == null || cardController.Card == null) return;
        
        Debug.Log($"[CardInteractionHandler] {cardController.Card.cardName} 드래그 발동 시도");
        
        // 이벤트 발행
        CardClickedEventData cardClickData = new CardClickedEventData(cardController);
        CardGameEvents.RaiseCardClicked(cardClickData);
        
        // 원래 자리로 복귀
        ReturnToHand();
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
    
    // 외부 접근자
    public Vector3 OriginalScale => originalScale;
}
