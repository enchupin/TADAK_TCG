using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class RestSceneEnhanceSlotLayoutView
{
    private const int CardsPerRow = 4;
    private const float ManualBottomPadding = 24f;

    private readonly ScrollRect enhanceScrollView;
    private readonly RectTransform enhanceListContent;
    private readonly GameObject enhanceCardPrefab;
    private readonly float enhanceCardScale;
    private readonly RectTransform[] enhanceSlotAnchors;
    private readonly List<GameObject> spawnedCards = new List<GameObject>();

    private sealed class SlotTemplate
    {
        public readonly RectTransform[] slots;
        public readonly int templateRowCount;
        public readonly Vector2 blockStep;

        public SlotTemplate(RectTransform[] slots, int templateRowCount, Vector2 blockStep)
        {
            this.slots = slots;
            this.templateRowCount = templateRowCount;
            this.blockStep = blockStep;
        }
    }

    public RestSceneEnhanceSlotLayoutView(
        ScrollRect enhanceScrollView,
        RectTransform enhanceListContent,
        GameObject enhanceCardPrefab,
        float enhanceCardScale,
        RectTransform[] enhanceSlotAnchors)
    {
        this.enhanceScrollView = enhanceScrollView;
        this.enhanceListContent = enhanceListContent;
        this.enhanceCardPrefab = enhanceCardPrefab;
        this.enhanceCardScale = enhanceCardScale;
        this.enhanceSlotAnchors = enhanceSlotAnchors;
    }

    public void Configure()
    {
        RectTransform content = ResolveEnhanceListContent();
        if (content == null)
        {
            SetEnhanceListVisible(false);
            return;
        }

        DisableAutoLayout(content);
        Hide();
    }

    public bool Show(IReadOnlyList<RestDeckEnhanceCandidate> candidates, Action<RestDeckEnhanceCandidate, int> onSelect)
    {
        RectTransform content = ResolveEnhanceListContent();
        if (content == null || enhanceCardPrefab == null)
        {
            return false;
        }

        SlotTemplate template = BuildSlotTemplate();
        if (template == null)
        {
            Clear();
            SetEnhanceListVisible(false);
            return false;
        }

        DisableAutoLayout(content);
        Clear();

        if (candidates == null || candidates.Count == 0)
        {
            ResizeContent(content, GetViewportHeight(content));
            FinalizeListOpen();
            return true;
        }

        float requiredHeight = GetViewportHeight(content);
        for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            RestDeckEnhanceCandidate candidate = candidates[candidateIndex];
            if (candidate == null)
            {
                continue;
            }

            requiredHeight = Mathf.Max(requiredHeight, SpawnCandidateRow(content, template, candidateIndex, candidate, onSelect));
        }

        ResizeContent(content, requiredHeight);
        FinalizeListOpen();
        return true;
    }

    public void Hide()
    {
        Clear();
        SetEnhanceListVisible(false);
    }

    private SlotTemplate BuildSlotTemplate()
    {
        RectTransform[] validSlots = CollectValidSlots();
        if (validSlots.Length < CardsPerRow || validSlots.Length % CardsPerRow != 0)
        {
            return null;
        }

        int templateRowCount = validSlots.Length / CardsPerRow;
        Vector2 blockStep = ComputeBlockStep(validSlots, templateRowCount);
        return new SlotTemplate(validSlots, templateRowCount, blockStep);
    }

    private RectTransform[] CollectValidSlots()
    {
        if (enhanceSlotAnchors == null || enhanceSlotAnchors.Length == 0)
        {
            return Array.Empty<RectTransform>();
        }

        List<RectTransform> validSlots = new List<RectTransform>(enhanceSlotAnchors.Length);
        for (int i = 0; i < enhanceSlotAnchors.Length; i++)
        {
            RectTransform slot = enhanceSlotAnchors[i];
            if (slot != null)
            {
                validSlots.Add(slot);
            }
        }

        return validSlots.ToArray();
    }

    private static Vector2 ComputeBlockStep(RectTransform[] slots, int templateRowCount)
    {
        if (slots == null || slots.Length < CardsPerRow * 2 || templateRowCount <= 1)
        {
            return Vector2.zero;
        }

        Vector2 accumulatedStep = Vector2.zero;
        int sampleCount = 0;
        for (int row = 0; row < templateRowCount - 1; row++)
        {
            for (int column = 0; column < CardsPerRow; column++)
            {
                int currentIndex = row * CardsPerRow + column;
                int nextIndex = currentIndex + CardsPerRow;
                accumulatedStep += slots[nextIndex].anchoredPosition - slots[currentIndex].anchoredPosition;
                sampleCount++;
            }
        }

        if (sampleCount <= 0)
        {
            return Vector2.zero;
        }

        return accumulatedStep / sampleCount * templateRowCount;
    }

    private float SpawnCandidateRow(
        RectTransform content,
        SlotTemplate template,
        int candidateIndex,
        RestDeckEnhanceCandidate candidate,
        Action<RestDeckEnhanceCandidate, int> onSelect)
    {
        if (content == null || template == null || candidate == null)
        {
            return GetViewportHeight(content);
        }

        int templateRowIndex = template.templateRowCount > 0
            ? candidateIndex % template.templateRowCount
            : 0;
        int blockIndex = template.templateRowCount > 0
            ? candidateIndex / template.templateRowCount
            : 0;

        Vector2 rowOffset = template.blockStep * blockIndex;
        float requiredHeight = 0f;

        requiredHeight = Mathf.Max(requiredHeight,
            SpawnCardAtSlot(content, template, templateRowIndex, 0, rowOffset, candidate.sourceCardId, null));

        List<int> routeCardIds = RestDeckEnhanceService.GetRandomEnhanceOptions(candidate, 3);
        for (int routeIndex = 0; routeIndex < routeCardIds.Count && routeIndex < 3; routeIndex++)
        {
            int routeCardId = routeCardIds[routeIndex];
            requiredHeight = Mathf.Max(requiredHeight,
                SpawnCardAtSlot(
                    content,
                    template,
                    templateRowIndex,
                    routeIndex + 1,
                    rowOffset,
                    routeCardId,
                    () => onSelect?.Invoke(candidate, routeCardId)));
        }

        return requiredHeight;
    }

    private float SpawnCardAtSlot(
        RectTransform content,
        SlotTemplate template,
        int templateRowIndex,
        int columnIndex,
        Vector2 rowOffset,
        int cardId,
        Action onClick)
    {
        Card card = CardManager.GetCardAsCard(cardId);
        if (card == null)
        {
            return 0f;
        }

        RectTransform slot = GetSlot(template, templateRowIndex, columnIndex);
        if (slot == null)
        {
            return 0f;
        }

        GameObject cardObject = UnityEngine.Object.Instantiate(enhanceCardPrefab, content);
        spawnedCards.Add(cardObject);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchorMin = slot.anchorMin;
            cardRect.anchorMax = slot.anchorMax;
            cardRect.pivot = slot.pivot;
            cardRect.anchoredPosition = slot.anchoredPosition + rowOffset;
            cardRect.localScale = Vector3.one * enhanceCardScale;
        }
        else
        {
            cardObject.transform.localScale = Vector3.one * enhanceCardScale;
        }

        CardController controller = cardObject.GetComponent<CardController>();
        if (controller == null)
        {
            UnityEngine.Object.Destroy(cardObject);
            return 0f;
        }

        controller.useInteractionHandler = false;
        if (controller.interactionHandler != null)
        {
            controller.interactionHandler.showPlayThreshold = false;
            controller.interactionHandler.enabled = false;
        }

        controller.Initialize(card);

        if (onClick != null)
        {
            CardSelectionClickHandler clickHandler = cardObject.GetComponent<CardSelectionClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = cardObject.AddComponent<CardSelectionClickHandler>();
            }

            clickHandler.Bind(controller, onClick);
        }

        float cardHeight = cardRect != null ? cardRect.rect.height * enhanceCardScale : 280f * enhanceCardScale;
        return -slot.anchoredPosition.y - rowOffset.y + cardHeight + ManualBottomPadding;
    }

    private static RectTransform GetSlot(SlotTemplate template, int templateRowIndex, int columnIndex)
    {
        if (template == null || template.slots == null)
        {
            return null;
        }

        int slotIndex = templateRowIndex * CardsPerRow + columnIndex;
        if (slotIndex < 0 || slotIndex >= template.slots.Length)
        {
            return null;
        }

        return template.slots[slotIndex];
    }

    private static void DisableAutoLayout(RectTransform content)
    {
        if (content == null)
        {
            return;
        }

        VerticalLayoutGroup verticalLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.enabled = false;
        }

        HorizontalLayoutGroup horizontalLayoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup != null)
        {
            horizontalLayoutGroup.enabled = false;
        }

        GridLayoutGroup gridLayoutGroup = content.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
        {
            gridLayoutGroup.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = content.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }
    }

    private RectTransform ResolveEnhanceListContent()
    {
        if (enhanceListContent != null)
        {
            return enhanceListContent;
        }

        return enhanceScrollView != null ? enhanceScrollView.content : null;
    }

    private void FinalizeListOpen()
    {
        SetEnhanceListVisible(true);
        Canvas.ForceUpdateCanvases();
        if (enhanceScrollView != null)
        {
            enhanceScrollView.verticalNormalizedPosition = 1f;
        }
    }

    private void ResizeContent(RectTransform content, float requiredHeight)
    {
        if (content == null)
        {
            return;
        }

        Vector2 sizeDelta = content.sizeDelta;
        sizeDelta.y = Mathf.Max(requiredHeight, GetViewportHeight(content));
        content.sizeDelta = sizeDelta;
    }

    private float GetViewportHeight(RectTransform content)
    {
        if (enhanceScrollView?.viewport != null)
        {
            return enhanceScrollView.viewport.rect.height;
        }

        return content != null ? Mathf.Max(content.rect.height, content.sizeDelta.y) : 0f;
    }

    private void SetEnhanceListVisible(bool visible)
    {
        if (enhanceScrollView != null)
        {
            enhanceScrollView.gameObject.SetActive(visible);
            return;
        }

        RectTransform content = ResolveEnhanceListContent();
        if (content != null)
        {
            content.gameObject.SetActive(visible);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                UnityEngine.Object.Destroy(spawnedCards[i]);
            }
        }

        spawnedCards.Clear();
    }
}
