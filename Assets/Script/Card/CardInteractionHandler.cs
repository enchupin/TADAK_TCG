using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
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
    
    [Header("Targeting")]
    private bool isTargetingMode = false;
    private TargetingArrow targetingArrow;
    
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

        // 화살표 생성
        GameObject arrowObj = new GameObject("TargetingArrow");
        targetingArrow = arrowObj.AddComponent<TargetingArrow>();
        targetingArrow.Initialize();
    }
    
    
    private void Start()
    {
        // 모든 컴포넌트의 Start()가 완료된 후에 체크하도록 지연
        Invoke(nameof(CheckAndCreateThresholdLine), 0.01f);

        if (targetingArrow != null && canvas != null)
        {
            targetingArrow.transform.SetParent(canvas.transform, false);
            targetingArrow.transform.SetAsLastSibling();
        }
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
    
    private bool RequiresTargeting()
    {
        if (cardUI == null) return false;
        CardController cc = cardUI.GetComponent<CardController>();
        if (cc == null || cc.Card == null || cc.Card.effects == null) return false;

        foreach (var effect in cc.Card.effects)
        {
            if (effect is ExecuteDamageEffect) return true;
            if (effect is DamageEffect damageEffect && damageEffect.target == TargetType.SingleEnemy) return true;
            if (effect is AttackEffect attackEffect && attackEffect.target == TargetType.SingleEnemy) return true;
        }
        return false;
    }


    #region Drag
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

        isTargetingMode = RequiresTargeting();

        if (isTargetingMode)
        {
            // 타겟팅 모드: 카드는 제자리에 두고 화살표만 활성화
            if (targetingArrow != null)
            {
                targetingArrow.transform.SetAsLastSibling();
                targetingArrow.gameObject.SetActive(true);
                targetingArrow.UpdateArrow(rectTransform.position, eventData.position);
            }
        }
        else
        {
            // 일반 모드: 카드 이동 준비
            CreatePlaceholder();
            layoutElement.ignoreLayout = true;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        
        if (isTargetingMode)
        {
            if (targetingArrow != null)
            {
                targetingArrow.UpdateArrow(rectTransform.position, eventData.position);
            }
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out Vector2 localPoint);
                
            rectTransform.position = canvas.transform.TransformPoint(localPoint);
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        isAnyCardDragging = false; // 전역 드래그 상태 비활성화
        
        if (isTargetingMode)
        {
            if (targetingArrow != null)
            {
                targetingArrow.gameObject.SetActive(false);
            }

            // 마우스 포인터 아래에 몬스터가 있는지 확인
            Monster targetMonster = null;
            if (eventData.hovered != null)
            {
                foreach (var go in eventData.hovered)
                {
                    Monster m = go.GetComponentInParent<Monster>();
                    if (m != null)
                    {
                        targetMonster = m;
                        break;
                    }
                }
            }

            if (targetMonster != null)
            {
                // 타겟을 찾았으므로 카드 사용
                // 현재는 단일 타겟 지정을 지원하지 않으므로 (임시로 0번째 몬스터 공격 중) 
                // 향후 BattleManager나 CardPlay에 타겟 정보를 넘길 수 있도록 이벤트 발행
                onCardPlayRequested?.Invoke();
            }
            // 허공에 놓았을 때는 카드가 이미 손패의 제자리에 머물러 있으므로 위치를 조정할 필요가 없습니다.
        }
        else
        {
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
        }
        
        // 어떤 모드든 드래그가 끝나면 원래 크기로 복원
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
