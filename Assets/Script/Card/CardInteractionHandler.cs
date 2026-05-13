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
    private readonly float cardHoverDuration = 0f;

    [Header("Drag Components")]
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private CardUI cardUI;
    private CardController cardController;

    [Header("Targeting")]
    private bool isTargetingMode = false;
    private TargetingArrow targetingArrow;
    private HandManager handManager;

    private int hoverSiblingIndex = -1;
    private bool isHoverSiblingOverridden = false;
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
        cardController = GetComponent<CardController>();
        handManager = ResolveHandManager();

        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    private void Start()
    {
        Invoke(nameof(CheckAndCreateThresholdLine), 0.01f);
    }

    private void OnDestroy()
    {
        ResolveHandManager()?.ClearHoveredCard(rectTransform, false);

        if (targetingArrow != null)
        {
            Destroy(targetingArrow.gameObject);
            targetingArrow = null;
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
            ResolveHandManager()?.SetHoveredCard(rectTransform);
            BringToFrontOnHover();
            base.OnPointerEnter(eventData);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging)
        {
            return;
        }

        RestoreSiblingAfterHover();
        ResolveHandManager()?.ClearHoveredCard(rectTransform);
        base.OnPointerExit(eventData);
    }

    private HandManager ResolveHandManager()
    {
        if (handManager != null)
        {
            return handManager;
        }

        handManager = GetComponentInParent<HandManager>();
        if (handManager == null && rectTransform != null)
        {
            handManager = HandManager.FindOwningHandManager(rectTransform);
        }

        return handManager;
    }

    private bool RequiresTargeting()
    {
        if (cardUI == null) return false;

        CardController cc = cardUI.GetComponent<CardController>();
        if (cc == null || cc.Card == null || cc.Card.effects == null) return false;

        bool hasSingleEnemyTarget = false;
        CollectTargetingFlags(cc.Card.effects, ref hasSingleEnemyTarget);
        return hasSingleEnemyTarget;
    }

    // 카드 이펙트 전체를 순회하고 단일 적 타겟 포함 여부를 수집합니다
    private void CollectTargetingFlags(System.Collections.Generic.List<ICardEffect> effects, ref bool hasSingleEnemyTarget)
    {
        if (effects == null) return;
        foreach (var nested in effects) {
            CollectTargetingFlags(nested, ref hasSingleEnemyTarget);
        }
    }

    // SingleEnemy를 가지는 모든 이펙트에 대해 타겟팅이 동작하도록 처리
    private void CollectTargetingFlags(ICardEffect effect, ref bool hasSingleEnemyTarget)
    {
        if (effect == null) return;

        if (effect is DamageEffect damageEffect) {
            if (damageEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }

            CollectTargetingFlags(damageEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is KillEffect killEffect) {
            if (killEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }
            return;
        }

        if (effect is RemoveBuffEffect removeBuffEffect) {
            if (removeBuffEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }
            return;
        }

        if (effect is MixBuffEffect mixBuffEffect) {
            if (mixBuffEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }
            return;
        }

        if (effect is EnemyHpLossHealPlayerEffect enemyHpLossHealPlayerEffect) {
            if (enemyHpLossHealPlayerEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }
            return;
        }

        if (effect is ChangeStatEffect changeStatEffect) {
            if (changeStatEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }

            CollectTargetingFlags(changeStatEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is AttackEffect attackEffect) {
            if (attackEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }

            CollectTargetingFlags(attackEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is BuffEffect buffEffect) {
            if (buffEffect.target == TargetType.SingleEnemy) {
                hasSingleEnemyTarget = true;
            }
            return;
        }

        if (effect is BarrierEffect barrierEffect) {
            CollectTargetingFlags(barrierEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is MoveEffect moveEffect) {
            CollectTargetingFlags(moveEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is ExhaustCardEffect exhaustCardEffect) {
            CollectTargetingFlags(exhaustCardEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is SelectCardEffect selectCardEffect) {
            CollectTargetingFlags(selectCardEffect.onActions, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is ConditionalEffect conditionalEffect) {
            CollectTargetingFlags(conditionalEffect.successEffects, ref hasSingleEnemyTarget);
            CollectTargetingFlags(conditionalEffect.failEffects, ref hasSingleEnemyTarget);
            return;
        }

        if (effect is RepeatEffect repeatEffect) {
            CollectTargetingFlags(repeatEffect.effectToRepeat, ref hasSingleEnemyTarget);
            return;
        }
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag()) return;

        isDragging = true;
        isAnyCardDragging = true;
        originalParent = rectTransform.parent;
        originalSiblingIndex = ConsumeHoverSiblingIndex();
        ResolveHandManager()?.ClearHoveredCard(rectTransform, false);
        originalAnchoredPosition = rectTransform.anchoredPosition;
        originalLocalPosition = rectTransform.localPosition;

        StopAnimation();
        transform.localScale = originalScale * cardHoverScale;

        isTargetingMode = RequiresTargeting();
        ClearPreviewTarget();

        if (isTargetingMode)
        {
            EnsureTargetingArrow();
            canvasGroup.blocksRaycasts = false;
            if (targetingArrow != null)
            {
                targetingArrow.transform.SetAsLastSibling();
                targetingArrow.gameObject.SetActive(true);
                targetingArrow.UpdateArrow(rectTransform.position, eventData.position);
            }

            UpdatePreviewTarget(eventData);
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

            UpdatePreviewTarget(eventData);
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
        bool playAccepted = false;

        if (isTargetingMode)
        {
            canvasGroup.blocksRaycasts = true;
            if (targetingArrow != null)
            {
                targetingArrow.gameObject.SetActive(false);
            }

            Monster targetMonster = ResolveHoveredMonster(eventData);
            ClearPreviewTarget();

            if (targetMonster != null)
            {
                playAccepted = RequestCardPlay(targetMonster);
            }

            if (playAccepted)
            {
                canvasGroup.blocksRaycasts = false;
            }
            else if (this != null && gameObject != null && rectTransform != null)
            {
                ReturnToHand();
            }
        }
        else
        {
            DestroyPlaceholder();

            if (eventData.position.y > Screen.height * playThresholdYRatio)
            {
                playAccepted = RequestCardPlay(null);
            }

            if (playAccepted)
            {
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                canvasGroup.blocksRaycasts = true;
                layoutElement.ignoreLayout = false;
                if (this != null && gameObject != null && rectTransform != null)
                {
                    ReturnToHand();
                }
            }
        }

        if (this == null || gameObject == null)
            return;

        StopAnimation();
        if (playAccepted)
        {
            transform.localScale = originalScale;
        }
        else
        {
            StartCoroutine(AnimateScale(originalScale));
        }
    }

    private bool RequestCardPlay(Monster targetMonster)
    {
        if (cardController != null)
        {
            cardController.ResetLastPlayRequestResult();
        }

        onCardPlayRequested?.Invoke(targetMonster);
        return cardController != null && cardController.LastPlayRequestAccepted;
    }

    public void PrepareAcceptedPlayAnimationStart()
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector3 playStartWorldPosition = rectTransform.position;

        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (canvas != null && rectTransform.parent != canvas.transform)
        {
            rectTransform.SetParent(canvas.transform, true);
        }

        rectTransform.position = playStartWorldPosition;

        transform.SetAsLastSibling();
    }

    private void UpdatePreviewTarget(PointerEventData eventData)
    {
        TrainingBattleManager manager = TrainingBattleManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.SetPreviewDescriptionTarget(ResolveHoveredMonster(eventData));
    }

    private void ClearPreviewTarget()
    {
        TrainingBattleManager.Instance?.ClearPreviewDescriptionTarget();
    }

    private Monster ResolveHoveredMonster(PointerEventData eventData)
    {
        Monster hoveredMonster = ResolveHoveredMonsterFromUI(eventData);
        if (hoveredMonster != null)
        {
            return hoveredMonster;
        }

        return ResolveHoveredMonsterFromPhysics(eventData);
    }

    private Monster ResolveHoveredMonsterFromUI(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return null;
        }

        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            Monster currentRaycastMonster = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<Monster>();
            if (currentRaycastMonster != null && !currentRaycastMonster.IsDead())
            {
                return currentRaycastMonster;
            }
        }

        if (eventData.hovered == null)
        {
            return null;
        }

        foreach (GameObject go in eventData.hovered)
        {
            Monster monster = go != null ? go.GetComponentInParent<Monster>() : null;
            if (monster != null && !monster.IsDead())
            {
                return monster;
            }
        }

        return null;
    }

    private Monster ResolveHoveredMonsterFromPhysics(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return null;
        }

        Camera targetCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            targetCamera = canvas.worldCamera;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return null;
        }

        Vector3 worldPoint = targetCamera.ScreenToWorldPoint(eventData.position);
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(new Vector2(worldPoint.x, worldPoint.y));
        foreach (Collider2D hitCollider in hitColliders)
        {
            Monster monster = hitCollider != null ? hitCollider.GetComponentInParent<Monster>() : null;
            if (monster != null && !monster.IsDead())
            {
                return monster;
            }
        }

        Ray pointerRay = targetCamera.ScreenPointToRay(eventData.position);
        RaycastHit[] hitResults = Physics.RaycastAll(pointerRay);
        foreach (RaycastHit hitResult in hitResults)
        {
            Monster monster = hitResult.collider != null ? hitResult.collider.GetComponentInParent<Monster>() : null;
            if (monster != null && !monster.IsDead())
            {
                return monster;
            }
        }

        return null;
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
        if (handManager != null)
        {
            handManager.UpdateHandCardPositions();
            return;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
        rectTransform.localPosition = originalLocalPosition;
    }

    private void BringToFrontOnHover()
    {
        if (rectTransform == null || rectTransform.parent == null || isHoverSiblingOverridden)
            return;

        hoverSiblingIndex = rectTransform.GetSiblingIndex();
        isHoverSiblingOverridden = true;
        rectTransform.SetAsLastSibling();
    }

    private void RestoreSiblingAfterHover()
    {
        if (isDragging || rectTransform == null || rectTransform.parent == null || !isHoverSiblingOverridden)
            return;

        int siblingIndex = Mathf.Clamp(hoverSiblingIndex, 0, rectTransform.parent.childCount - 1);
        rectTransform.SetSiblingIndex(siblingIndex);
        hoverSiblingIndex = -1;
        isHoverSiblingOverridden = false;
    }

    private int ConsumeHoverSiblingIndex()
    {
        int siblingIndex = isHoverSiblingOverridden ? hoverSiblingIndex : transform.GetSiblingIndex();
        hoverSiblingIndex = -1;
        isHoverSiblingOverridden = false;
        return siblingIndex;
    }

    private bool CanStartDrag()
    {
        if (cardUI == null)
            return false;

        if (!cardUI.IsPlayable)
            return false;

        TrainingBattleManager manager = TrainingBattleManager.Instance;
        if (manager != null && !manager.CanInteractWithCards())
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

    private void EnsureTargetingArrow()
    {
        if (targetingArrow != null)
        {
            return;
        }

        GameObject arrowObj = new GameObject("TargetingArrow");
        targetingArrow = arrowObj.AddComponent<TargetingArrow>();
        targetingArrow.Initialize();

        if (canvas != null)
        {
            targetingArrow.transform.SetParent(canvas.transform, false);
            targetingArrow.transform.SetAsLastSibling();
        }
    }
}
