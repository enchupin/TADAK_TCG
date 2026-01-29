using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 카드 입력(드래그, 클릭)을 처리하는 컨트롤러
/// CardUI에서 로직을 분리하여 드래그 앤 드롭 기능을 구현함
/// 호버 효과도 함께 처리합니다.
/// </summary>
public class CardInputControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CardUI cardUI;
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;

    private int originalSiblingIndex;
    private bool isDragging = false;
    private GameObject placeholder; // 드래그 중 레이아웃 유지를 위한 Placeholder

    // 카드를 발동시킬 Y축 임계값 (화면 높이 비율)
    private readonly float PLAY_THRESHOLD_Y_RATIO = 0.3f;


    [Header("Hover Settings")]
    private readonly float hoverScale = 1.4f; // 호버링 시 확대 크기
    private readonly float hoverDuration = 0.15f; // 호버링 애니메이션 시간 (초)
    private Vector3 originalScale;
    private Coroutine scaleCoroutine; // 현재 실행 중인 스케일 애니메이션 코루틴

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
        
        // 호버 효과 초기화
        originalScale = transform.localScale;
    }



    private void Start()
    {
        // 임시 테스트 코드
        if (showPlayThreshold && debugLineObject == null && canvas != null)
        {
            CreateDebugThresholdLine();
        }
    }

    /// <summary>
    /// 호버링 스케일 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator ScaleAnimation(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hoverDuration); // 0~1 사이 값
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale; // 정확한 목표값으로 설정
    }



    /// <summary>
    /// 테스트 전용 메서드 (카드 발동 임계 포지션 시각화)
    /// </summary>
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





    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardUI == null || !cardUI.IsPlayable) return;

        isDragging = true;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Placeholder 생성 (다른 카드들의 위치를 유지하기 위해)
        CreatePlaceholder();
        
        // 레이아웃 무시 (자유로운 이동)
        layoutElement.ignoreLayout = true;
        
        // 레이캐스트 차단 해제 (드롭 위치 감지 등을 위해)
        canvasGroup.blocksRaycasts = false;
        
        // 렌더링 순서 최상위로
        transform.SetAsLastSibling();
    }



    /// <summary>
    /// 드래그 중
    /// </summary>
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


    /// <summary>
    /// 드래그 완료
    /// </summary>
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
        
        // Placeholder 제거
        DestroyPlaceholder();
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
    
    /// <summary>
    /// Placeholder 생성 (다른 카드들의 위치 유지)
    /// </summary>
    private void CreatePlaceholder()
    {
        if (placeholder != null) return; // 이미 존재하면 생성하지 않음
        
        // 호버링 스케일 초기화 (원래 크기로)
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        transform.localScale = originalScale;
        
        // 빈 GameObject 생성
        placeholder = new GameObject("CardPlaceholder");
        placeholder.transform.SetParent(transform.parent, false);
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);
        
        // RectTransform 추가 및 설정 복사
        var placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = rectTransform.sizeDelta;
        
        // LayoutElement 추가 및 원래 설정 복사
        var placeholderLayout = placeholder.AddComponent<LayoutElement>();
        
        // 원래 카드의 LayoutElement 설정을 그대로 복사
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
            // LayoutElement가 없는 경우 rect 크기 사용
            placeholderLayout.preferredWidth = rectTransform.rect.width;
            placeholderLayout.preferredHeight = rectTransform.rect.height;
        }
        
        Debug.Log($"[CardInputControl] Placeholder 생성: {placeholder.name}, 크기: {placeholderLayout.preferredWidth}x{placeholderLayout.preferredHeight}");
    }
    
    /// <summary>
    /// Placeholder 제거
    /// </summary>
    private void DestroyPlaceholder()
    {
        if (placeholder != null)
        {
            Debug.Log($"[CardInputControl] Placeholder 제거: {placeholder.name}");
            Destroy(placeholder);
            placeholder = null;
        }
    }

    


    /// <summary>
    /// 카드가 포인터되었을 때
    /// 호버링 효과
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && cardUI != null) {
            // 기존 애니메이션이 실행 중이면 중지
            if (scaleCoroutine != null) {
                StopCoroutine(scaleCoroutine);
            }
            
            // 새로운 애니메이션 시작
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale * hoverScale));
        }
    }


    /// <summary>
    /// 카드가 포인터 아웃되었을 때
    /// 호버링 효과 해제
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            // 기존 애니메이션이 실행 중이면 중지
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            
            // 새로운 애니메이션 시작
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
        }
    }




    /// <summary>
    /// 클릭 시 발동 (현재는 사용하지 않음)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData) {
        // 클릭 시에는 발동하지 않음 (필요 시 확대/상세보기 로직 추가)
        if (!isDragging && cardUI != null && cardUI.card != null) {
            Debug.Log($"[CardInputControl] {cardUI.card.cardName} 클릭됨 (발동 안함)");
        }
    }

}
