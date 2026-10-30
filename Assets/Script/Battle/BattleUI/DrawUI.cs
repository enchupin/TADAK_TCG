using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DrawUI : MonoBehaviour
{
    [SerializeField] private GameObject drawAnimationPrefab;
    [SerializeField] private Transform drawStartPosition;
    [SerializeField] private Transform animationRoot;
    [SerializeField] private string fallbackDrawStartObjectName = "DeckBtn";
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float staggerDelay = 0.08f;
    [FormerlySerializedAs("firstPosition")]
    [SerializeField] private Transform firstDrawPosition;
    [FormerlySerializedAs("secondPosition")]
    [SerializeField] private Transform secondDrawPosition;

    [Header("Play Animation")]
    [SerializeField] private Transform playCenterPosition;
    [SerializeField] private Transform discardTargetPosition;
    [SerializeField] private Transform firstDiscardPosition;
    [SerializeField] private Transform secondDiscardPosition;
    [SerializeField] private float playCenterMoveDuration = 0.2f;
    [SerializeField] private float playCenterPauseDuration = 0.2f;
    [SerializeField] private float discardMoveDuration = 0.35f;
    [Tooltip("중앙에서 사용 대기 중인 카드가 앞 카드 뒤로 한 장마다 밀리는 거리 (0이면 완전히 겹침)")]
    [SerializeField] private Vector2 playQueueStackOffset = new Vector2(16f, -8f);

    [Header("Debug")]
    [SerializeField] private bool showDebugPathOnMove;
    [SerializeField] private Transform debugTargetPosition;
    [SerializeField] private Color debugPathColor = Color.cyan;
    [SerializeField] private int debugPathSegments = 24;
    [SerializeField] private float debugPathLineThickness = 3f;
    [SerializeField] private Vector2 debugPathPointSize = new Vector2(8f, 8f);
    [SerializeField] private float debugPathLifetime = 2f;

    // 카드 사용 연출의 중앙 지점 (손패 선택에서 고른 카드를 놓는 위치로도 사용)
    public Transform PlayCenterPosition => playCenterPosition;

    // 사용 연출 대기열 항목 (중앙에 겹쳐 대기하다가 순서대로 버린 더미로 이동)
    private class UseAnimationEntry
    {
        public GameObject CardObject;
        public CanvasGroup CanvasGroup;
        public Coroutine Move;
        public bool Arrived;
    }

    private readonly System.Collections.Generic.List<UseAnimationEntry> useAnimationQueue = new System.Collections.Generic.List<UseAnimationEntry>();
    private Coroutine useAnimationQueueRoutine;
    // 대기열을 떠나 버린 더미로 날아가는 중인 카드 수
    private int useDiscardFlightCount;

    // 중앙 대기 중이거나 버린 더미로 이동 중인 사용 연출이 있는지
    public bool HasPendingUseAnimations => isActiveAndEnabled && (useAnimationQueue.Count > 0 || useDiscardFlightCount > 0);

    private Transform cachedFallbackDrawStartPosition;
    private GameObject debugPathRoot;
    private Coroutine clearDebugPathCoroutine;

    private readonly struct CanvasGroupState
    {
        public readonly float Alpha;
        public readonly bool Interactable;
        public readonly bool BlocksRaycasts;

        public CanvasGroupState(CanvasGroup canvasGroup)
        {
            Alpha = canvasGroup.alpha;
            Interactable = canvasGroup.interactable;
            BlocksRaycasts = canvasGroup.blocksRaycasts;
        }

        public void Restore(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = Alpha;
            canvasGroup.interactable = Interactable;
            canvasGroup.blocksRaycasts = BlocksRaycasts;
        }
    }


    private Vector3 FourPointBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
        float u = 1f - t;

        return
            u * u * u * a +
            3f * u * u * t * b +
            3f * u * t * t * c +
            t * t * t * d;
    }

    public Coroutine PlayMove(GameObject cardObject, int sequenceIndex = 0)
    {
        if (cardObject == null || !isActiveAndEnabled || drawAnimationPrefab == null || firstDrawPosition == null || secondDrawPosition == null)
        {
            LogDrawAnimationMissingReference(cardObject);
            return null;
        }

        Transform target = cardObject.transform;
        GameObject animationCard = Instantiate(drawAnimationPrefab, ResolveAnimationRoot(target), false);
        animationCard.name = $"{drawAnimationPrefab.name}_DrawAnimation";
        animationCard.transform.SetAsLastSibling();
        DisableAnimationCardInput(animationCard);

        CanvasGroup animationCanvasGroup = animationCard.GetComponent<CanvasGroup>();
        if (animationCanvasGroup == null)
        {
            animationCanvasGroup = animationCard.AddComponent<CanvasGroup>();
        }

        animationCanvasGroup.interactable = false;
        animationCanvasGroup.blocksRaycasts = false;

        CanvasGroup originalCanvasGroup = cardObject.GetComponent<CanvasGroup>();
        if (originalCanvasGroup == null)
        {
            originalCanvasGroup = cardObject.AddComponent<CanvasGroup>();
        }

        CanvasGroupState originalState = new CanvasGroupState(originalCanvasGroup);
        originalCanvasGroup.alpha = 0f;
        originalCanvasGroup.interactable = false;
        originalCanvasGroup.blocksRaycasts = false;

        Vector3 startPos = ResolveDrawStartPosition(target.position);
        animationCard.transform.position = startPos;
        if (showDebugPathOnMove)
        {
            ShowDebugPath(target, startPos);
        }

        return StartCoroutine(MoveCard(animationCard, target, originalCanvasGroup, originalState, startPos, sequenceIndex));
    }

    private void LogDrawAnimationMissingReference(GameObject cardObject)
    {
        if (cardObject == null)
        {
            Debug.LogWarning("[DrawUI] 드로우 연출 대상 카드가 비어 있습니다");
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[DrawUI] DrawUI가 비활성화되어 드로우 연출을 실행할 수 없습니다");
            return;
        }

        if (drawAnimationPrefab == null)
        {
            Debug.LogWarning("[DrawUI] Draw Animation Prefab이 연결되어 있지 않습니다");
            return;
        }

        if (firstDrawPosition == null)
        {
            Debug.LogWarning("[DrawUI] First Draw Position이 연결되어 있지 않습니다");
            return;
        }

        if (secondDrawPosition == null)
        {
            Debug.LogWarning("[DrawUI] Second Draw Position이 연결되어 있지 않습니다");
        }
    }

    // 사용 연출을 대기열에 넣음 (연출을 시작하지 못하면 false)
    public bool PlayUseToDiscard(GameObject cardObject)
    {
        if (cardObject == null || !isActiveAndEnabled || drawAnimationPrefab == null || discardTargetPosition == null)
        {
            return false;
        }

        Transform target = cardObject.transform;
        target.SetParent(ResolveAnimationRoot(target), true);
        target.SetAsLastSibling();
        // 먼저 사용한 카드가 위에 보이도록 대기 카드는 앞 카드 뒤에 겹침
        GameObject lastQueuedCard = useAnimationQueue.Count > 0 ? useAnimationQueue[useAnimationQueue.Count - 1].CardObject : null;
        if (lastQueuedCard != null && lastQueuedCard.transform.parent == target.parent)
        {
            target.SetSiblingIndex(lastQueuedCard.transform.GetSiblingIndex());
        }
        target.localRotation = Quaternion.identity;
        DisableAnimationCardInput(cardObject);

        CanvasGroup canvasGroup = cardObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        UseAnimationEntry entry = new UseAnimationEntry { CardObject = cardObject, CanvasGroup = canvasGroup };
        useAnimationQueue.Add(entry);
        MoveUseEntryToSlot(entry, useAnimationQueue.Count - 1);
        if (useAnimationQueueRoutine == null)
        {
            useAnimationQueueRoutine = StartCoroutine(RunUseAnimationQueue());
        }

        return true;
    }

    // 비활성화로 연출 코루틴이 멈추면 대기 중인 카드가 화면에 남지 않도록 정리
    private void OnDisable()
    {
        foreach (UseAnimationEntry entry in useAnimationQueue)
        {
            if (entry.CardObject != null)
            {
                Destroy(entry.CardObject);
            }
        }

        useAnimationQueue.Clear();
        useAnimationQueueRoutine = null;
        useDiscardFlightCount = 0;
    }

    private IEnumerator MoveCard(
        GameObject animationCard,
        Transform target,
        CanvasGroup originalCanvasGroup,
        CanvasGroupState originalState,
        Vector3 startPos,
        int sequenceIndex)
    {
        float delay = Mathf.Max(0, sequenceIndex) * staggerDelay;
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (animationCard == null || target == null)
        {
            originalState.Restore(originalCanvasGroup);
            if (animationCard != null)
            {
                Destroy(animationCard);
            }

            yield break;
        }

        float t = 0f;
        float moveDuration = Mathf.Max(0.01f, duration);

        while (t < 1f) {
            if (animationCard == null || target == null)
            {
                originalState.Restore(originalCanvasGroup);
                if (animationCard != null)
                {
                    Destroy(animationCard);
                }

                yield break;
            }

            t += Time.deltaTime / moveDuration;
            float clampedT = Mathf.Clamp01(t);
            Vector3 endPos = target.position;

            animationCard.transform.position = FourPointBezier(
                startPos,
                firstDrawPosition.position,
                secondDrawPosition.position,
                endPos,
                clampedT
            );

            yield return null;
        }

        originalState.Restore(originalCanvasGroup);

        if (animationCard != null)
        {
            Destroy(animationCard);
        }
    }

    // 대기열 칸 위치 (0번 칸이 중앙, 뒤 칸일수록 playQueueStackOffset만큼 밀림)
    private Vector3 ResolveUseQueueSlotPosition(Transform cardTransform, int slotIndex)
    {
        Vector3 centerPosition = ResolvePlayCenterPosition(cardTransform.position);
        if (slotIndex <= 0 || cardTransform.parent == null)
        {
            return centerPosition;
        }

        return centerPosition + cardTransform.parent.TransformVector(playQueueStackOffset * slotIndex);
    }

    private void MoveUseEntryToSlot(UseAnimationEntry entry, int slotIndex)
    {
        if (entry.CardObject == null)
        {
            return;
        }

        if (entry.Move != null)
        {
            StopCoroutine(entry.Move);
        }

        entry.Arrived = false;
        entry.Move = StartCoroutine(MoveUseEntry(entry, ResolveUseQueueSlotPosition(entry.CardObject.transform, slotIndex)));
    }

    private IEnumerator MoveUseEntry(UseAnimationEntry entry, Vector3 slotPosition)
    {
        Transform cardTransform = entry.CardObject != null ? entry.CardObject.transform : null;
        // 이미 그 칸에 있으면 이동 시간 없이 바로 도착 처리
        if (cardTransform != null && (cardTransform.position - slotPosition).sqrMagnitude > 0.0001f)
        {
            yield return MoveStraight(cardTransform, slotPosition, playCenterMoveDuration);
        }

        entry.Move = null;
        entry.Arrived = true;
    }

    // 맨 앞 카드가 중앙에 도착하면 잠시 멈췄다가 버린 더미로 보내고, 다음 카드를 한 칸씩 앞당김
    private IEnumerator RunUseAnimationQueue()
    {
        while (useAnimationQueue.Count > 0)
        {
            UseAnimationEntry entry = useAnimationQueue[0];
            while (entry.CardObject != null && !entry.Arrived)
            {
                yield return null;
            }

            if (entry.CardObject != null && playCenterPauseDuration > 0f)
            {
                yield return new WaitForSeconds(playCenterPauseDuration);
            }

            useAnimationQueue.RemoveAt(0);
            if (entry.CardObject != null)
            {
                StartCoroutine(MoveUsedCardToDiscard(entry.CardObject, entry.CanvasGroup, discardTargetPosition));
            }

            for (int i = 0; i < useAnimationQueue.Count; i++)
            {
                MoveUseEntryToSlot(useAnimationQueue[i], i);
            }
        }

        useAnimationQueueRoutine = null;
    }

    private IEnumerator MoveUsedCardToDiscard(GameObject cardObject, CanvasGroup cardCanvasGroup, Transform discardTarget)
    {
        if (cardObject == null || discardTarget == null)
        {
            if (cardObject != null)
            {
                Destroy(cardObject);
            }

            yield break;
        }

        GameObject discardAnimationCard = CreateUseDiscardAnimationCard(cardObject.transform);
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = 0f;
        }

        useDiscardFlightCount++;
        try
        {
            yield return MoveToDiscard(discardAnimationCard.transform, discardTarget);
        }
        finally
        {
            useDiscardFlightCount = Mathf.Max(0, useDiscardFlightCount - 1);
            if (discardAnimationCard != null)
            {
                Destroy(discardAnimationCard);
            }

            if (cardObject != null)
            {
                Destroy(cardObject);
            }
        }
    }

    private GameObject CreateUseDiscardAnimationCard(Transform source)
    {
        GameObject animationCard = Instantiate(drawAnimationPrefab, ResolveAnimationRoot(source), false);
        animationCard.name = $"{drawAnimationPrefab.name}_UseAnimation";
        animationCard.SetActive(true);
        animationCard.transform.position = source.position;
        animationCard.transform.SetAsLastSibling();
        DisableAnimationCardInput(animationCard);

        CanvasGroup animationCanvasGroup = animationCard.GetComponent<CanvasGroup>();
        if (animationCanvasGroup == null)
        {
            animationCanvasGroup = animationCard.AddComponent<CanvasGroup>();
        }

        animationCanvasGroup.alpha = 1f;
        animationCanvasGroup.interactable = false;
        animationCanvasGroup.blocksRaycasts = false;

        return animationCard;
    }

    private IEnumerator MoveStraight(Transform target, Vector3 endPos, float moveDuration)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 startPos = target.position;
        float elapsed = 0f;
        float resolvedDuration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < 1f)
        {
            if (target == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime / resolvedDuration;
            target.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(elapsed));
            yield return null;
        }

        target.position = endPos;
    }

    private IEnumerator MoveToDiscard(Transform target, Transform discardTarget)
    {
        if (target == null || discardTarget == null)
        {
            yield break;
        }

        Vector3 startPos = target.position;
        float elapsed = 0f;
        float resolvedDuration = Mathf.Max(0.01f, discardMoveDuration);

        while (elapsed < 1f)
        {
            if (target == null || discardTarget == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime / resolvedDuration;
            float clampedT = Mathf.Clamp01(elapsed);
            Vector3 endPos = discardTarget.position;

            target.position = FourPointBezier(
                startPos,
                ResolveFirstDiscardPoint(startPos),
                ResolveSecondDiscardPoint(endPos),
                endPos,
                clampedT
            );

            yield return null;
        }

        if (target != null && discardTarget != null)
        {
            target.position = discardTarget.position;
        }
    }

    private void DisableAnimationCardInput(GameObject animationCard)
    {
        if (animationCard == null)
        {
            return;
        }

        CardController[] cardControllers = animationCard.GetComponentsInChildren<CardController>(true);
        foreach (CardController cardController in cardControllers)
        {
            cardController.enabled = false;
        }

        CardInteractionHandler[] interactionHandlers = animationCard.GetComponentsInChildren<CardInteractionHandler>(true);
        foreach (CardInteractionHandler interactionHandler in interactionHandlers)
        {
            interactionHandler.enabled = false;
        }
    }

    [ContextMenu("Debug Show Draw Path")]
    public void DebugShowDrawPath()
    {
        if (debugTargetPosition == null)
        {
            Debug.LogWarning("[DrawUI] 드로우 경로 디버그 대상이 비어 있습니다");
            return;
        }

        Vector3 startPos = ResolveDrawStartPosition(debugTargetPosition.position);
        ShowDebugPath(debugTargetPosition, startPos);
    }

    [ContextMenu("Debug Clear Draw Path")]
    public void DebugClearDrawPath()
    {
        ClearDebugPath();
    }

    public void DebugShowDrawPath(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("[DrawUI] 드로우 경로 디버그 대상이 비어 있습니다");
            return;
        }

        Vector3 startPos = ResolveDrawStartPosition(target.position);
        ShowDebugPath(target, startPos);
    }

    private void ShowDebugPath(Transform target, Vector3 startPos)
    {
        if (target == null || firstDrawPosition == null || secondDrawPosition == null)
        {
            return;
        }

        ClearDebugPath();

        Transform root = ResolveAnimationRoot(target);
        debugPathRoot = new GameObject("DrawPathDebug");
        debugPathRoot.transform.SetParent(root, false);
        debugPathRoot.transform.SetAsLastSibling();

        int segmentCount = Mathf.Max(2, debugPathSegments);
        Vector3 previousPoint = startPos;
        CreateDebugPoint(previousPoint, 1.4f);

        for (int i = 1; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 endPos = target.position;
            Vector3 currentPoint = FourPointBezier(
                startPos,
                firstDrawPosition.position,
                secondDrawPosition.position,
                endPos,
                t
            );

            CreateDebugSegment(previousPoint, currentPoint);
            CreateDebugPoint(currentPoint, 1f);
            previousPoint = currentPoint;
        }

        if (Application.isPlaying && debugPathLifetime > 0f)
        {
            clearDebugPathCoroutine = StartCoroutine(ClearDebugPathAfterDelay());
        }
    }

    private IEnumerator ClearDebugPathAfterDelay()
    {
        yield return new WaitForSeconds(debugPathLifetime);
        clearDebugPathCoroutine = null;
        ClearDebugPath();
    }

    private void ClearDebugPath()
    {
        if (clearDebugPathCoroutine != null)
        {
            StopCoroutine(clearDebugPathCoroutine);
            clearDebugPathCoroutine = null;
        }

        if (debugPathRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(debugPathRoot);
        }
        else
        {
            DestroyImmediate(debugPathRoot);
        }

        debugPathRoot = null;
    }

    private void CreateDebugPoint(Vector3 position, float scale)
    {
        if (debugPathRoot == null)
        {
            return;
        }

        GameObject point = CreateDebugGraphic("PathPoint");
        RectTransform rectTransform = point.GetComponent<RectTransform>();
        rectTransform.position = position;
        rectTransform.sizeDelta = debugPathPointSize * scale;
    }

    private void CreateDebugSegment(Vector3 from, Vector3 to)
    {
        if (debugPathRoot == null)
        {
            return;
        }

        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            return;
        }

        GameObject segment = CreateDebugGraphic("PathSegment");
        RectTransform rectTransform = segment.GetComponent<RectTransform>();
        rectTransform.position = (from + to) * 0.5f;
        rectTransform.sizeDelta = new Vector2(length, debugPathLineThickness);
        rectTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private GameObject CreateDebugGraphic(string objectName)
    {
        GameObject graphicObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        graphicObject.transform.SetParent(debugPathRoot.transform, false);

        RawImage rawImage = graphicObject.GetComponent<RawImage>();
        rawImage.texture = Texture2D.whiteTexture;
        rawImage.color = debugPathColor;
        rawImage.raycastTarget = false;

        RectTransform rectTransform = graphicObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        return graphicObject;
    }

    private Transform ResolveAnimationRoot(Transform target)
    {
        if (animationRoot != null)
        {
            return animationRoot;
        }

        Canvas targetCanvas = target != null ? target.GetComponentInParent<Canvas>() : null;
        return targetCanvas != null ? targetCanvas.transform : transform;
    }

    private Vector3 ResolveDrawStartPosition(Vector3 fallbackPosition)
    {
        Transform startPosition = drawStartPosition != null
            ? drawStartPosition
            : ResolveFallbackDrawStartPosition();

        return startPosition != null ? startPosition.position : fallbackPosition;
    }

    private Vector3 ResolvePlayCenterPosition(Vector3 fallbackPosition)
    {
        return playCenterPosition != null ? playCenterPosition.position : fallbackPosition;
    }

    private Vector3 ResolveFirstDiscardPoint(Vector3 startPos)
    {
        return firstDiscardPosition != null ? firstDiscardPosition.position : startPos;
    }

    private Vector3 ResolveSecondDiscardPoint(Vector3 endPos)
    {
        return secondDiscardPosition != null ? secondDiscardPosition.position : endPos;
    }

    private Transform ResolveFallbackDrawStartPosition()
    {
        if (cachedFallbackDrawStartPosition != null)
        {
            return cachedFallbackDrawStartPosition;
        }

        if (string.IsNullOrEmpty(fallbackDrawStartObjectName))
        {
            return null;
        }

        GameObject fallbackObject = GameObject.Find(fallbackDrawStartObjectName);
        cachedFallbackDrawStartPosition = fallbackObject != null ? fallbackObject.transform : null;
        return cachedFallbackDrawStartPosition;
    }

}
