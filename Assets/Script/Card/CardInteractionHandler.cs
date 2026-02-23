using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class CardPlayEvent : UnityEvent<Monster> { }

/// <summary>
/// Handles card hover, drag, targeting, and play request events.
/// </summary>
public class CardInteractionHandler : UIHoverEffect,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Events")]
    [SerializeField] private CardPlayEvent onCardPlayRequested;

    [Header("Drag Settings")]
    private readonly float playThresholdYRatio = 0.3f;
    private static bool isAnyCardDragging = false;
    private bool isDragging = false;

    [Header("Debug")]
    public bool showPlayThreshold = true;
    private Color thresholdColor = new Color(1f, 0f, 0f, 0.5f);
    private static GameObject debugLineObject;

    [Header("Hover Settings")]
    private readonly float cardHoverScale = 1.4f;
    private readonly float cardHoverDuration = 0.15f;

    [Header("Drag Components")]
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private CardUI cardUI;

    [Header("Targeting")]
    private bool isTargetingMode = false;
    private TargetingArrow targetingArrow;

    private int originalSiblingIndex;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalPosition;
    private GameObject placeholder;

    public CardPlayEvent OnCardPlayRequested => onCardPlayRequested;

    protected override void Awake()
    {
        base.Awake();

        SetHoverScale(cardHoverScale);
        SetAnimationDuration(cardHoverDuration);

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();
        cardUI = GetComponent<CardUI>();

        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();

        GameObject arrowObj = new GameObject("TargetingArrow");
        targetingArrow = arrowObj.AddComponent<TargetingArrow>();
        targetingArrow.Initialize();
    }

    private void Start()
    {
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

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && !isAnyCardDragging)
        {
            base.OnPointerEnter(eventData);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

        isDragging = true;
        isAnyCardDragging = true;
        originalParent = rectTransform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalLocalPosition = rectTransform.localPosition;

        StopAnimation();
        transform.localScale = originalScale;

        isTargetingMode = RequiresTargeting();

        if (isTargetingMode)
        {
            if (targetingArrow != null)
            {
                targetingArrow.transform.SetAsLastSibling();
                targetingArrow.gameObject.SetActive(true);
                targetingArrow.UpdateArrow(rectTransform.position, eventData.position);
            }
        }
        else
        {
            CreatePlaceholder();
            layoutElement.ignoreLayout = true;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

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
        if (!isDragging)
            return;

        isDragging = false;
        isAnyCardDragging = false;

        if (isTargetingMode)
        {
            if (targetingArrow != null)
            {
                targetingArrow.gameObject.SetActive(false);
            }

            Monster targetMonster = null;
            if (eventData.hovered != null)
            {
                foreach (GameObject go in eventData.hovered)
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
                onCardPlayRequested?.Invoke(targetMonster);
            }

            // Always restore transform after a play request.
            // If play succeeds, card object is destroyed by battle manager.
            if (this != null && gameObject != null && rectTransform != null)
            {
                ReturnToHand();
            }
        }
        else
        {
            canvasGroup.blocksRaycasts = true;
            layoutElement.ignoreLayout = false;
            DestroyPlaceholder();

            if (eventData.position.y > Screen.height * playThresholdYRatio)
            {
                onCardPlayRequested?.Invoke(null);
                if (this != null && gameObject != null && rectTransform != null)
                {
                    ReturnToHand();
                }
            }
            else
            {
                ReturnToHand();
            }
        }

        if (this == null || gameObject == null)
            return;

        StopAnimation();
        StartCoroutine(AnimateScale(originalScale));
    }

    private void ReturnToHand()
    {
        if (rectTransform == null)
            return;

        if (originalParent != null && rectTransform.parent != originalParent)
        {
            rectTransform.SetParent(originalParent, false);
        }

        int siblingIndex = originalSiblingIndex;
        if (rectTransform.parent != null)
        {
            siblingIndex = Mathf.Clamp(originalSiblingIndex, 0, rectTransform.parent.childCount - 1);
        }

        rectTransform.SetSiblingIndex(siblingIndex);
        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localPosition = originalLocalPosition;
    }

    private bool CanStartDrag()
    {
        if (cardUI == null)
            return false;

        TrainingBattleManager manager = TrainingBattleManager.Instance;
        if (manager != null && !manager.CanUseIdentityAbility())
            return false;

        return true;
    }

    private void CreatePlaceholder()
    {
        if (placeholder != null) return;

        placeholder = new GameObject("CardPlaceholder");
        placeholder.transform.SetParent(transform.parent, false);
        placeholder.transform.SetSiblingIndex(originalSiblingIndex);

        RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = rectTransform.sizeDelta;

        LayoutElement placeholderLayout = placeholder.AddComponent<LayoutElement>();
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

    private void CreateDebugThresholdLine()
    {
        debugLineObject = new GameObject("Debug_ThresholdLine");
        debugLineObject.transform.SetParent(canvas.transform, false);
        debugLineObject.transform.SetAsLastSibling();

        Image img = debugLineObject.AddComponent<Image>();
        img.color = thresholdColor;
        img.raycastTarget = false;

        RectTransform rt = debugLineObject.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, playThresholdYRatio);
        rt.anchorMax = new Vector2(1f, playThresholdYRatio);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, 4f);
        rt.anchoredPosition = Vector2.zero;
    }
}
