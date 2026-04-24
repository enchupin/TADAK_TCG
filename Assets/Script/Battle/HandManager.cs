using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages hand cards and corresponding UI objects.
/// </summary>
public class HandManager : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject cardUIPrefab;

    [Header("Layout")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private float maxHandWidth = 1100f;
    [SerializeField] private float baseCardSpacing = 170f;
    [SerializeField] private float curveHeight = 45f;
    [SerializeField] private float maxCardRotation = 12f;

    [Header("Hand")]
    private readonly List<Card> handCardList = new List<Card>();

    public void AddCard(Card card)
    {
        if (card == null)
            return;

        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("[HandManager] CardUI prefab or hand container is missing.");
            return;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        handCardList.Add(card);
        InstantiateCardUI(card);
        UpdateHandCardPositions();
    }

    public void AddCard(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return;

        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("[HandManager] CardUI prefab or hand container is missing.");
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            Card processedCard = TrainingBattleManager.Instance != null
                ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
                : card;
            handCardList.Add(processedCard);
            InstantiateCardUI(processedCard);
        }

        UpdateHandCardPositions();
    }

    private void InstantiateCardUI(Card card)
    {
        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);
        CardController controller = cardObj.GetComponent<CardController>();

        if (controller != null)
        {
            controller.Initialize(card);
        }
        else
        {
            Debug.LogWarning("[HandManager] CardController is missing on card prefab.");
        }
    }

    /// <summary>
    /// Adds cards without enabling interaction/drag-play.
    /// </summary>
    public void AddCardWithoutInputController(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return;

        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("[HandManager] CardUI prefab or hand container is missing.");
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            Card processedCard = TrainingBattleManager.Instance != null
                ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
                : card;
            handCardList.Add(processedCard);
            InstantiateCardUIWithoutInputController(processedCard);
        }

        UpdateHandCardPositions();
    }

    private void InstantiateCardUIWithoutInputController(Card card)
    {
        GameObject cardObj = Instantiate(cardUIPrefab, handContainer);
        CardController controller = cardObj.GetComponent<CardController>();

        if (controller != null)
        {
            controller.Initialize(card);
            controller.useInteractionHandler = false;
        }
        else
        {
            Debug.LogWarning("[HandManager] CardController is missing on card prefab.");
        }
    }

    public void UpdateHandCardPositions()
    {
        if (handContainer == null)
            return;

        List<RectTransform> handCardRects = new List<RectTransform>(handCardList.Count);
        foreach (Transform child in handContainer)
        {
            CardController controller = child.GetComponent<CardController>();
            if (controller == null || controller.Card == null || !handCardList.Contains(controller.Card))
                continue;

            if (child is RectTransform rectTransform)
            {
                handCardRects.Add(rectTransform);
            }
        }

        int cardCount = handCardRects.Count;
        if (cardCount == 0)
            return;

        float spacing = 0f;
        if (cardCount > 1)
        {
            float widthLimitedSpacing = maxHandWidth / (cardCount - 1);
            spacing = Mathf.Min(baseCardSpacing, widthLimitedSpacing);
        }

        float centerIndex = (cardCount - 1) * 0.5f;
        for (int i = 0; i < cardCount; i++)
        {
            RectTransform cardRect = handCardRects[i];
            float normalized = cardCount == 1 ? 0f : (i / (cardCount - 1f)) * 2f - 1f;
            float x = (i - centerIndex) * spacing;
            float y = curveHeight * (1f - normalized * normalized) - curveHeight;
            float rotationZ = -normalized * maxCardRotation;

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }


    public void RemoveCardFromHand(CardUI cardUI)
    {
        if (cardUI == null)
        {
            Debug.LogWarning("[HandManager] CardUI is null.");
            return;
        }

        CardController controller = cardUI.GetComponent<CardController>();
        if (controller == null || controller.Card == null)
        {
            Debug.LogWarning("[HandManager] CardController or Card is missing.");
            Destroy(cardUI.gameObject);
            return;
        }

        Card card = controller.Card;
        if (handCardList.Contains(card))
        {
            handCardList.Remove(card);
            Debug.Log($"[HandManager] Removed from hand: {card.cardName}");
            UpdateHandCardPositions();
        }

        Destroy(cardUI.gameObject);
    }

    public bool RemoveCard(Card card)
    {
        if (card == null) {
            return false;
        }

        if (!handCardList.Contains(card)) {
            return false;
        }

        handCardList.Remove(card);
        UpdateHandCardPositions();

        CardUI cardUI = GetCardUI(card);
        if (cardUI != null) {
            Destroy(cardUI.gameObject);
        }

        return true;
    }

    public List<Card> ClearHand()
    {
        List<Card> discardedCards = new List<Card>(handCardList);
        handCardList.Clear();

        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }

        return discardedCards;
    }

    public int GetCardCount()
    {
        return handCardList.Count;
    }

    public List<Card> GetHandCards()
    {
        return new List<Card>(handCardList);
    }

    public int GetHandCount()
    {
        return GetCardCount();
    }

    public CardUI GetCardUI(Card card)
    {
        foreach (Transform child in handContainer)
        {
            CardController controller = child.GetComponent<CardController>();
            if (controller != null && controller.Card == card)
            {
                return controller.cardUI;
            }
        }

        return null;
    }

    public void RefreshCardDisplay(Card card)
    {
        if (card == null)
            return;

        CardUI cardUI = GetCardUI(card);
        if (cardUI != null)
        {
            cardUI.UpdateDisplay(card);
        }
    }

    public void RefreshCardDisplays(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return;

        foreach (Card card in cards)
        {
            RefreshCardDisplay(card);
        }
    }

    /// <summary>
    /// Updates every hand card's playable state.
    /// </summary>
    public void RefreshCardPlayability(Func<Card, bool> canPlayCard, bool canInteract)
    {
        if (handContainer == null)
            return;

        foreach (Transform child in handContainer)
        {
            CardController controller = child.GetComponent<CardController>();
            if (controller == null || controller.Card == null || controller.cardUI == null)
                continue;

            bool isPlayable = canInteract && (canPlayCard == null || canPlayCard(controller.Card));
            controller.isPlayable = isPlayable;
            controller.cardUI.SetPlayable(isPlayable);
        }
    }
}
