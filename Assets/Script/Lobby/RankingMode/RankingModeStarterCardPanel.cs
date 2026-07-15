using System.Collections.Generic;
using UnityEngine;

public class RankingModeStarterCardPanel : MonoBehaviour
{
    private const int CardsPerRow = 4;
    private const int StarterCardCount = 7;
    private const int RowCount = 2;

    [Header("기본카드 표시 설정")]
    [SerializeField] private RectTransform cardPanel;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float cardScale = 0.55f;
    [SerializeField] private Vector2 cardSpacing = new Vector2(10f, 10f);
    [SerializeField] private Vector2 panelPadding = new Vector2(10f, 10f);

    private readonly List<GameObject> createdCards = new List<GameObject>();

    public void ShowStarterCards(Character character)
    {
        if (!ValidateReferences())
        {
            return;
        }

        ClearCards();

        List<int> starterCardIds = CharacterManager.GetStarterCardIds(character);
        if (starterCardIds == null || starterCardIds.Count == 0)
        {
            Debug.LogWarning($"[RankingModeStarterCardPanel] 기본카드 목록을 찾을 수 없습니다: {character}");
            return;
        }

        int displayCount = Mathf.Min(StarterCardCount, starterCardIds.Count);
        for (int i = 0; i < displayCount; i++)
        {
            Card card = CardManager.GetCardAsCard(starterCardIds[i]);
            if (card == null)
            {
                Debug.LogWarning($"[RankingModeStarterCardPanel] 카드 데이터를 찾을 수 없습니다: {starterCardIds[i]}");
                continue;
            }

            CreateCard(card, createdCards.Count);
        }
    }

    public void ClearCards()
    {
        for (int i = createdCards.Count - 1; i >= 0; i--)
        {
            if (createdCards[i] != null)
            {
                Destroy(createdCards[i]);
            }
        }

        createdCards.Clear();
    }

    private bool ValidateReferences()
    {
        if (cardPanel == null)
        {
            Debug.LogError("[RankingModeStarterCardPanel] 카드가 들어갈 패널이 연결되지 않았습니다");
            return false;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("[RankingModeStarterCardPanel] 카드 프리팹이 연결되지 않았습니다");
            return false;
        }

        Vector2 panelSize = ResolvePanelSize();
        if (panelSize.x <= 0f || panelSize.y <= 0f)
        {
            Debug.LogError("[RankingModeStarterCardPanel] 카드 패널 크기를 확인할 수 없습니다");
            return false;
        }

        return true;
    }

    private void CreateCard(Card card, int cardIndex)
    {
        GameObject cardObject = Instantiate(cardPrefab, cardPanel, false);
        cardObject.name = $"RankingModeStarterCard_{card.cardId}";

        RectTransform cardRect = cardObject.transform as RectTransform;
        Vector2 cardSize = ResolveCardSize(cardRect);
        Vector2 panelSize = ResolvePanelSize();
        float resolvedScale = ResolveCardScale(cardSize, panelSize);
        SetupCardTransform(cardRect, cardIndex, cardSize, resolvedScale, panelSize);
        SetupCardController(cardObject, card);
        CaptureCardScale(cardObject);

        createdCards.Add(cardObject);
    }

    private Vector2 ResolveCardSize(RectTransform cardRect)
    {
        if (cardRect == null)
        {
            return new Vector2(200f, 280f);
        }

        Vector2 size = cardRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = cardRect.sizeDelta;
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            return new Vector2(200f, 280f);
        }

        return size;
    }

    private Vector2 ResolvePanelSize()
    {
        if (cardPanel == null)
        {
            return Vector2.zero;
        }

        Vector2 panelSize = cardPanel.rect.size;
        if (panelSize.x <= 0f || panelSize.y <= 0f)
        {
            panelSize = cardPanel.sizeDelta;
        }

        return panelSize;
    }

    private float ResolveCardScale(Vector2 cardSize, Vector2 panelSize)
    {
        float availableWidth = panelSize.x - panelPadding.x * 2f - cardSpacing.x * (CardsPerRow - 1);
        float availableHeight = panelSize.y - panelPadding.y * 2f - cardSpacing.y * (RowCount - 1);
        float widthScale = availableWidth / (cardSize.x * CardsPerRow);
        float heightScale = availableHeight / (cardSize.y * RowCount);
        float fitScale = Mathf.Min(widthScale, heightScale);

        return Mathf.Max(0.01f, Mathf.Min(cardScale, fitScale));
    }

    private void SetupCardTransform(
        RectTransform cardRect,
        int cardIndex,
        Vector2 cardSize,
        float resolvedScale,
        Vector2 panelSize)
    {
        if (cardRect == null)
        {
            return;
        }

        int column = cardIndex % CardsPerRow;
        int row = cardIndex / CardsPerRow;
        float scaledWidth = cardSize.x * resolvedScale;
        float scaledHeight = cardSize.y * resolvedScale;
        float gridWidth = CardsPerRow * scaledWidth + (CardsPerRow - 1) * cardSpacing.x;
        float startX = Mathf.Max(panelPadding.x, (panelSize.x - gridWidth) * 0.5f);

        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(0f, 1f);
        cardRect.pivot = new Vector2(0f, 1f);
        cardRect.anchoredPosition = new Vector2(
            startX + column * (scaledWidth + cardSpacing.x),
            -panelPadding.y - row * (scaledHeight + cardSpacing.y));
        cardRect.localScale = Vector3.one * resolvedScale;
    }

    private void SetupCardController(GameObject cardObject, Card card)
    {
        CardController controller = cardObject.GetComponent<CardController>();
        if (controller != null)
        {
            controller.useInteractionHandler = false;
            if (controller.interactionHandler != null)
            {
                controller.interactionHandler.showPlayThreshold = false;
                controller.interactionHandler.enabled = false;
            }

            controller.Initialize(card);
            return;
        }

        CardUI cardUI = cardObject.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.UpdateDisplay(card);
            return;
        }

        Debug.LogWarning("[RankingModeStarterCardPanel] 카드 프리팹에서 CardController 또는 CardUI를 찾을 수 없습니다");
    }

    private void CaptureCardScale(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }

        UIHoverEffect[] hoverEffects = cardObject.GetComponentsInChildren<UIHoverEffect>(true);
        foreach (UIHoverEffect hoverEffect in hoverEffects)
        {
            if (hoverEffect == null)
            {
                continue;
            }

            hoverEffect.StopAnimation();
            hoverEffect.CaptureCurrentScaleAsOriginal();
        }
    }
}
