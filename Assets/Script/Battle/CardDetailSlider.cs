using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 카드 확인 패널: 기본형과 강화형(최대 5종)을 왼쪽부터 순서대로 놓고 슬라이드로 넘겨 봄
/// 가운데 = 선택된 카드 + 효과 툴팁, 좌우 카드는 순서대로 겹쳐 놓고 클릭 · 드래그 · 방향키로 넘김
/// </summary>
public class CardDetailSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 가운데 카드와 첫 옆 카드 사이 거리 (카드 너비 대비)
    private const float FirstSideOffsetRatio = 0.75f;
    // 옆 카드끼리 겹쳐 보이는 간격 (카드 너비 대비)
    private const float StackOffsetRatio = 0.22f;
    private const float SideScale = 0.8f;
    private const float MoveSharpness = 14f;
    // 이만큼 끌면 한 칸 넘김 (카드 너비 대비)
    private const float DragStepRatio = 0.35f;
    // 효과 툴팁이 여러 개면 이 간격으로 아래로 나열
    private const float TooltipGap = 4f;

    private GameObject cardPrefab;
    private Transform cardParent;
    private UIModalPanel ownerPanel;
    private RectTransform cardRoot;
    private readonly List<CardController> cardViews = new List<CardController>();
    private int centerIndex;
    private float dragAccumulated;
    // 전투 화면 버프 툴팁과 같은 패널 복제본 (가운데 카드 효과마다 하나)
    private RectTransform tooltipRoot;
    private readonly List<(RectTransform panel, TextMeshProUGUI text)> tooltipPanels = new List<(RectTransform, TextMeshProUGUI)>();
    private int shownTooltipCount;
    private bool hasLoggedMissingTooltipView;

    // 드래그는 패널 전체에서 받고, 카드는 지정한 부모(비우면 패널) 아래에 놓음
    public void Configure(GameObject prefab, Transform parent, UIModalPanel panel)
    {
        cardPrefab = prefab;
        cardParent = parent != null ? parent : transform;
        ownerPanel = panel;
    }

    public void Show(Card clickedCard)
    {
        Clear();
        if (clickedCard == null || cardPrefab == null)
        {
            return;
        }

        List<Card> familyCards = ResolveFamilyCards(clickedCard, out int clickedIndex);
        RectTransform root = EnsureCardRoot();
        foreach (Card card in familyCards)
        {
            CardController view = CreateCardView(root, card);
            if (view != null)
            {
                cardViews.Add(view);
            }
        }

        centerIndex = Mathf.Clamp(clickedIndex, 0, Mathf.Max(0, cardViews.Count - 1));
        Debug.Log($"[CardDetailSlider] {clickedCard.cardName}({clickedCard.cardId}) 계열 {cardViews.Count}장, 가운데 {centerIndex}번");
        ApplyLayout(true);
    }

    public void Clear()
    {
        foreach (CardController view in cardViews)
        {
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }
        cardViews.Clear();
        centerIndex = 0;
        dragAccumulated = 0f;
    }

    public void HideTooltips()
    {
        foreach (CardController view in cardViews)
        {
            view?.cardUI?.HideBuffTooltip();
        }

        foreach ((RectTransform panel, TextMeshProUGUI _) in tooltipPanels)
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }
        shownTooltipCount = 0;
    }

    private void Update()
    {
        if (cardViews.Count == 0)
        {
            return;
        }

        // 다른 패널(설정 등)이 위에 열려 있으면 방향키로 넘기지 않음
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (ownerPanel == null || UIModalStack.IsTop(ownerPanel)))
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) Focus(centerIndex - 1);
            else if (keyboard.rightArrowKey.wasPressedThisFrame) Focus(centerIndex + 1);
        }

        ApplyLayout(false);
        PositionTooltips();
    }

    private void Focus(int index)
    {
        int clamped = Mathf.Clamp(index, 0, cardViews.Count - 1);
        if (clamped == centerIndex)
        {
            return;
        }

        centerIndex = clamped;
        SettingsManager.PlayPanelToggleSound();
        RefreshTooltips();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragAccumulated = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float step = GetCardWidth() * DragStepRatio;
        if (step <= 0f)
        {
            return;
        }

        // 왼쪽으로 끌면 오른쪽 카드(다음 강화형)가 가운데로 옴
        dragAccumulated += eventData.delta.x / Mathf.Max(0.0001f, GetCanvasScale());
        while (dragAccumulated <= -step)
        {
            dragAccumulated += step;
            Focus(centerIndex + 1);
        }
        while (dragAccumulated >= step)
        {
            dragAccumulated -= step;
            Focus(centerIndex - 1);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragAccumulated = 0f;
    }

    // 기본형 + 강화형(카드 ID 순)을 왼쪽부터 배치, 클릭한 카드는 현재 상태 그대로 사용
    private static List<Card> ResolveFamilyCards(Card clickedCard, out int clickedIndex)
    {
        List<Card> result = new List<Card>();
        clickedIndex = 0;

        int suffix = Mathf.Abs(clickedCard.cardId) % 10;
        int baseId = clickedCard.cardId - (clickedCard.cardId >= 0 ? suffix : -suffix);
        // 전투 중 카드는 강화 · 복제 과정에서 강화 ID 목록이 바뀔 수 있어 원본 데이터 기준으로 찾음
        Card baseTemplate = CardManager.GetCardAsCard(baseId) ?? (baseId == clickedCard.cardId ? clickedCard : null);
        List<int> familyIds = new List<int> { baseId };
        if (baseTemplate?.enforceCardIds != null)
        {
            List<int> upgradeIds = new List<int>(baseTemplate.enforceCardIds);
            upgradeIds.Sort();
            foreach (int upgradeId in upgradeIds)
            {
                if (!familyIds.Contains(upgradeId))
                {
                    familyIds.Add(upgradeId);
                }
            }
        }

        // 계열을 찾지 못하면 클릭한 카드 1장만 표시
        if (!familyIds.Contains(clickedCard.cardId))
        {
            result.Add(clickedCard);
            return result;
        }

        foreach (int cardId in familyIds)
        {
            if (cardId == clickedCard.cardId)
            {
                clickedIndex = result.Count;
                result.Add(clickedCard);
                continue;
            }

            Card template = CardManager.GetCardAsCard(cardId);
            if (template == null)
            {
                continue;
            }

            LocalizationManager.ApplyToRuntimeCard(template);
            result.Add(template);
        }
        return result;
    }

    private RectTransform EnsureCardRoot()
    {
        if (cardRoot != null)
        {
            return cardRoot;
        }

        GameObject rootObject = new GameObject("DetailSlideCards", typeof(RectTransform));
        cardRoot = (RectTransform)rootObject.transform;
        cardRoot.SetParent(cardParent != null ? cardParent : transform, false);
        cardRoot.anchorMin = new Vector2(0.5f, 0.5f);
        cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        cardRoot.sizeDelta = Vector2.zero;
        cardRoot.anchoredPosition = Vector2.zero;
        // 닫기 버튼 등 패널의 다른 자식보다 아래에 그림
        cardRoot.SetAsFirstSibling();
        return cardRoot;
    }

    private CardController CreateCardView(RectTransform root, Card card)
    {
        GameObject cardObject = Instantiate(cardPrefab, root);
        CardController controller = cardObject.GetComponent<CardController>();
        if (controller == null)
        {
            Debug.LogWarning("[CardDetailSlider] 카드 확인 프리팹에 CardController가 없습니다");
            Destroy(cardObject);
            return null;
        }

        RectTransform rect = (RectTransform)cardObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        controller.useInteractionHandler = false;
        if (controller.interactionHandler != null)
        {
            controller.interactionHandler.showPlayThreshold = false;
            controller.interactionHandler.enabled = false;
        }
        controller.Initialize(card);
        controller.isPlayable = true;
        controller.cardUI?.SetPlayable(true);

        // 옆 카드를 누르면 그 카드가 가운데로
        CardSelectionClickHandler clickHandler = cardObject.GetComponent<CardSelectionClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = cardObject.AddComponent<CardSelectionClickHandler>();
        }
        clickHandler.Bind(controller, () => Focus(cardViews.IndexOf(controller)));
        return controller;
    }

    private void ApplyLayout(bool immediate)
    {
        float width = GetCardWidth();
        float t = immediate ? 1f : 1f - Mathf.Exp(-MoveSharpness * Time.unscaledDeltaTime);
        for (int i = 0; i < cardViews.Count; i++)
        {
            CardController view = cardViews[i];
            if (view == null)
            {
                continue;
            }

            int offset = i - centerIndex;
            int distance = Mathf.Abs(offset);
            float x = distance == 0
                ? 0f
                : Mathf.Sign(offset) * width * (FirstSideOffsetRatio + StackOffsetRatio * (distance - 1));
            float scale = distance == 0 ? 1f : SideScale;

            RectTransform rect = (RectTransform)view.transform;
            rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(x, 0f), t);
            rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * scale, t);
        }

        // 가운데에 가까울수록 위에 그림 (가운데가 맨 위)
        List<CardController> order = new List<CardController>(cardViews);
        order.Sort((a, b) => Mathf.Abs(cardViews.IndexOf(b) - centerIndex).CompareTo(Mathf.Abs(cardViews.IndexOf(a) - centerIndex)));
        for (int i = 0; i < order.Count; i++)
        {
            order[i]?.transform.SetSiblingIndex(i);
        }

        if (immediate)
        {
            RefreshTooltips();
        }
    }

    // 툴팁은 가운데 카드만 표시 (전투 화면 버프 툴팁과 같은 패널을 효과마다 하나씩 아래로 나열)
    private void RefreshTooltips()
    {
        HideTooltips();
        CardUI centerUI = centerIndex >= 0 && centerIndex < cardViews.Count ? cardViews[centerIndex]?.cardUI : null;
        if (centerUI == null)
        {
            return;
        }

        TooltipView view = TooltipView.Instance;
        if (view == null)
        {
            if (!hasLoggedMissingTooltipView)
            {
                hasLoggedMissingTooltipView = true;
                Debug.LogWarning("[CardDetailSlider] 전투 툴팁 뷰가 없어 카드 효과 툴팁을 표시하지 않습니다");
            }
            return;
        }

        List<string> entries = centerUI.BuildTooltipEntries();
        RectTransform root = EnsureTooltipRoot();
        for (int i = 0; i < entries.Count; i++)
        {
            if (i >= tooltipPanels.Count)
            {
                RectTransform copy = view.CreatePanelCopy(root, out TextMeshProUGUI copyText);
                if (copy == null || copyText == null)
                {
                    break;
                }
                tooltipPanels.Add((copy, copyText));
            }

            (RectTransform panel, TextMeshProUGUI text) = tooltipPanels[i];
            // 비활성 상태에서는 TMP 크기 계산이 맞지 않을 수 있어 먼저 켬
            panel.gameObject.SetActive(true);
            view.FitPanel(panel, text, entries[i]);
            shownTooltipCount = i + 1;
        }

        PositionTooltips();
    }

    // 가운데 카드의 툴팁 자리(카드 안 툴팁 패널 좌측 상단)부터 아래로 나열, 화면 아래를 넘으면 위로 올림
    private void PositionTooltips()
    {
        if (shownTooltipCount == 0 || tooltipRoot == null || centerIndex >= cardViews.Count)
        {
            return;
        }

        CardController center = cardViews[centerIndex];
        RectTransform anchor = center != null && center.cardUI != null ? center.cardUI.TooltipAnchor : null;
        if (anchor == null && center != null)
        {
            anchor = (RectTransform)center.transform;
        }
        if (anchor == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        Vector2 topLeft = tooltipRoot.InverseTransformPoint(corners[1]);

        float totalHeight = 0f;
        for (int i = 0; i < shownTooltipCount; i++)
        {
            totalHeight += tooltipPanels[i].panel.rect.height + (i > 0 ? TooltipGap : 0f);
        }

        Canvas canvas = tooltipRoot.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
            float margin = TooltipView.Instance != null ? TooltipView.Instance.ScreenMargin : 8f;
            float screenBottom = tooltipRoot.InverseTransformPoint(corners[0]).y + margin;
            float screenTop = tooltipRoot.InverseTransformPoint(corners[1]).y - margin;
            topLeft.y = Mathf.Min(screenTop, Mathf.Max(topLeft.y, screenBottom + totalHeight));
        }

        float y = topLeft.y;
        for (int i = 0; i < shownTooltipCount; i++)
        {
            RectTransform panel = tooltipPanels[i].panel;
            panel.localPosition = new Vector3(topLeft.x, y, 0f);
            y -= panel.rect.height + TooltipGap;
        }
    }

    private RectTransform EnsureTooltipRoot()
    {
        if (tooltipRoot != null)
        {
            return tooltipRoot;
        }

        GameObject rootObject = new GameObject("DetailSlideTooltips", typeof(RectTransform));
        tooltipRoot = (RectTransform)rootObject.transform;
        tooltipRoot.SetParent(cardParent != null ? cardParent : transform, false);
        tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRoot.sizeDelta = Vector2.zero;
        tooltipRoot.anchoredPosition = Vector2.zero;
        // 카드 위, 닫기 버튼 등 패널의 다른 자식 아래
        int cardRootIndex = cardRoot != null ? cardRoot.GetSiblingIndex() : 0;
        tooltipRoot.SetSiblingIndex(cardRootIndex + 1);
        return tooltipRoot;
    }

    private float GetCardWidth()
    {
        foreach (CardController view in cardViews)
        {
            if (view != null)
            {
                return ((RectTransform)view.transform).rect.width;
            }
        }
        return 0f;
    }

    private float GetCanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
    }

    private void OnDisable()
    {
        HideTooltips();
    }
}
