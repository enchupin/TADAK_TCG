using System.Collections.Generic;
using UnityEngine;

public class RankingModeStarterCardPanel : MonoBehaviour
{
    private const int StarterCardCount = 7;

    [Header("기본카드 표시 설정")]
    [SerializeField] private RectTransform cardPanel;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private DictionaryCardLayout cardLayout;

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
                createdCards[i].SetActive(false);
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

        Vector2 panelSize = cardPanel.rect.size;
        if (panelSize.x <= 0f || panelSize.y <= 0f)
        {
            Debug.LogError("[RankingModeStarterCardPanel] 카드 패널 크기를 확인할 수 없습니다");
            return false;
        }

        if (cardLayout == null || cardLayout.transform != cardPanel || !cardLayout.HasCardPositions)
        {
            Debug.LogError("[RankingModeStarterCardPanel] 씬 카드 배치 위치가 연결되지 않았습니다");
            return false;
        }

        return true;
    }

    private void CreateCard(Card card, int cardIndex)
    {
        GameObject cardObject = Instantiate(cardPrefab, cardPanel, false);
        cardObject.name = $"RankingModeStarterCard_{card.cardId}";

        RectTransform cardRect = cardObject.transform as RectTransform;
        if (!cardLayout.PlaceCard(cardRect, cardIndex))
        {
            cardObject.SetActive(false);
            Destroy(cardObject);
            return;
        }

        SetupCardController(cardObject, card);
        CaptureCardScale(cardObject);

        createdCards.Add(cardObject);
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
