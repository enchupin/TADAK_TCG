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

    [Header("Draw Animation")]
    [SerializeField] private DrawUI drawUI;
    [SerializeField] private bool playDrawAnimation = true;

    [Header("Layout")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private float maxHandWidth = 1100f;
    [SerializeField] private float baseCardSpacing = 170f;
    [SerializeField] private float curveHeight = 45f;
    [SerializeField] private float maxCardRotation = 12f;

    [Header("Hand")]
    private readonly List<Card> handCardList = new List<Card>();
    private int drawAnimationFrame = -1;
    private int drawAnimationSequenceIndex;

    private readonly struct HandCardSortEntry
    {
        public readonly Card Card;
        public readonly int OriginalIndex;

        public HandCardSortEntry(Card card, int originalIndex)
        {
            Card = card;
            OriginalIndex = originalIndex;
        }
    }

    public void AddCard(Card card)
    {
        AddCard(card, false);
    }

    public Coroutine AddCard(Card card, bool animateDraw)
    {
        if (card == null)
            return null;

        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("[HandManager] CardUI prefab or hand container is missing.");
            return null;
        }

        card = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
            : card;
        InsertCardInHandOrder(card);
        GameObject cardObj = InstantiateCardUI(card);
        UpdateHandCardPositions();
        return TryPlayDrawAnimation(cardObj, animateDraw);
    }

    public void AddCard(List<Card> cards)
    {
        AddCard(cards, false);
    }

    public void AddCard(List<Card> cards, bool animateDraw)
    {
        if (cards == null || cards.Count == 0)
            return;

        if (cardUIPrefab == null || handContainer == null)
        {
            Debug.LogError("[HandManager] CardUI prefab or hand container is missing.");
            return;
        }

        List<GameObject> createdCardObjects = animateDraw ? new List<GameObject>() : null;
        foreach (Card card in cards)
        {
            if (card == null)
                continue;

            Card processedCard = TrainingBattleManager.Instance != null
                ? TrainingBattleManager.Instance.ApplyPersistentUpgradeToCard(card)
                : card;
            InsertCardInHandOrder(processedCard);
            GameObject cardObj = InstantiateCardUI(processedCard);
            createdCardObjects?.Add(cardObj);
        }

        UpdateHandCardPositions();

        if (createdCardObjects == null)
        {
            return;
        }

        foreach (GameObject cardObj in createdCardObjects)
        {
            TryPlayDrawAnimation(cardObj, true);
        }
    }

    private GameObject InstantiateCardUI(Card card)
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

        return cardObj;
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
            InsertCardInHandOrder(processedCard);
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

    private Coroutine TryPlayDrawAnimation(GameObject cardObj, bool animateDraw)
    {
        if (!animateDraw || !playDrawAnimation || cardObj == null || drawUI == null)
        {
            return null;
        }

        return drawUI.PlayMove(cardObj, GetNextDrawAnimationSequenceIndex());
    }

    private int GetNextDrawAnimationSequenceIndex()
    {
        if (drawAnimationFrame != Time.frameCount)
        {
            drawAnimationFrame = Time.frameCount;
            drawAnimationSequenceIndex = 0;
        }

        return drawAnimationSequenceIndex++;
    }

    public void UpdateHandCardPositions()
    {
        if (handContainer == null)
            return;

        SortHandCardsByCharacterOrder();
        List<RectTransform> handCardRects = GetHandCardRectsInHandOrder();

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
            cardRect.SetSiblingIndex(i);
            cardRect.anchoredPosition = new Vector2(x, y);
            cardRect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        }
    }

    private List<RectTransform> GetHandCardRectsInHandOrder()
    {
        List<RectTransform> handCardRects = new List<RectTransform>(handCardList.Count);
        List<RectTransform> usedRects = new List<RectTransform>(handCardList.Count);

        foreach (Card handCard in handCardList)
        {
            RectTransform rectTransform = FindUnusedCardRect(handCard, usedRects);
            if (rectTransform == null)
                continue;

            handCardRects.Add(rectTransform);
            usedRects.Add(rectTransform);
        }

        return handCardRects;
    }

    private RectTransform FindUnusedCardRect(Card card, List<RectTransform> usedRects)
    {
        if (card == null)
            return null;

        foreach (Transform child in handContainer)
        {
            RectTransform rectTransform = child as RectTransform;
            if (rectTransform == null || usedRects.Contains(rectTransform))
                continue;

            CardController controller = child.GetComponent<CardController>();
            if (controller != null && ReferenceEquals(controller.Card, card))
            {
                return rectTransform;
            }
        }

        return null;
    }

    private void InsertCardInHandOrder(Card card)
    {
        if (card == null)
        {
            return;
        }

        SortHandCardsByCharacterOrder();

        int targetOrder = GetCharacterSortOrder(card.character);
        int insertIndex = handCardList.Count;
        for (int i = 0; i < handCardList.Count; i++)
        {
            Card handCard = handCardList[i];
            if (handCard == null)
            {
                continue;
            }

            if (GetCharacterSortOrder(handCard.character) > targetOrder)
            {
                insertIndex = i;
                break;
            }
        }

        handCardList.Insert(insertIndex, card);
    }

    private void SortHandCardsByCharacterOrder()
    {
        if (handCardList.Count <= 1)
        {
            return;
        }

        List<HandCardSortEntry> sortEntries = new List<HandCardSortEntry>(handCardList.Count);
        for (int i = 0; i < handCardList.Count; i++)
        {
            sortEntries.Add(new HandCardSortEntry(handCardList[i], i));
        }

        sortEntries.Sort(CompareHandCardSortEntry);

        handCardList.Clear();
        foreach (HandCardSortEntry sortEntry in sortEntries)
        {
            handCardList.Add(sortEntry.Card);
        }
    }

    private int CompareHandCardSortEntry(HandCardSortEntry a, HandCardSortEntry b)
    {
        int orderCompare = GetCharacterSortOrder(a.Card?.character ?? Character.Monster)
            .CompareTo(GetCharacterSortOrder(b.Card?.character ?? Character.Monster));
        if (orderCompare != 0)
        {
            return orderCompare;
        }

        return a.OriginalIndex.CompareTo(b.OriginalIndex);
    }

    private int GetCharacterSortOrder(Character character)
    {
        if (character == Character.Monster)
        {
            return int.MaxValue;
        }

        List<Character> selectedCharacters = SelectedButtonControl.selectedCharacterList;
        if (selectedCharacters != null)
        {
            int selectedIndex = selectedCharacters.IndexOf(character);
            if (selectedIndex >= 0)
            {
                return selectedIndex;
            }
        }

        return 1000 + (int)character;
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

    public Coroutine RemoveCardFromHandWithUseAnimation(CardUI cardUI)
    {
        if (cardUI == null)
        {
            Debug.LogWarning("[HandManager] CardUI is null.");
            return null;
        }

        CardController controller = cardUI.GetComponent<CardController>();
        if (controller == null || controller.Card == null)
        {
            Debug.LogWarning("[HandManager] CardController or Card is missing.");
            Destroy(cardUI.gameObject);
            return null;
        }

        Card card = controller.Card;
        if (handCardList.Contains(card))
        {
            handCardList.Remove(card);
            Debug.Log($"[HandManager] Removed from hand: {card.cardName}");
            UpdateHandCardPositions();
        }

        Coroutine useAnimation = drawUI != null
            ? drawUI.PlayUseToDiscard(cardUI.gameObject)
            : null;

        if (useAnimation == null)
        {
            Destroy(cardUI.gameObject);
        }

        return useAnimation;
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
