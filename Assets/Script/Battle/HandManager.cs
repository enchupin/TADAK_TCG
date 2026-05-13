using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages hand cards and corresponding UI objects.
/// </summary>
public class HandManager : MonoBehaviour
{
    private static readonly List<HandManager> ActiveManagers = new List<HandManager>();

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

    [Header("Hover Layout")]
    [SerializeField] private float hoverSpreadOffset = 45f;
    [SerializeField] private float secondHoverSpreadOffset = 30f;
    [SerializeField] private float thirdHoverSpreadOffset = 15f;
    [SerializeField] private float hoverSpreadDuration = 0.08f;

    [Header("Hand")]
    private readonly List<Card> handCardList = new List<Card>();
    private int drawAnimationFrame = -1;
    private int drawAnimationSequenceIndex;
    private RectTransform hoveredCardRect;
    private Coroutine handLayoutAnimation;

    private void OnEnable()
    {
        if (!ActiveManagers.Contains(this))
        {
            ActiveManagers.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveManagers.Remove(this);
        hoveredCardRect = null;
        StopHandLayoutAnimation();
    }

    public static HandManager FindOwningHandManager(RectTransform cardRect)
    {
        if (cardRect == null)
        {
            return null;
        }

        for (int i = ActiveManagers.Count - 1; i >= 0; i--)
        {
            HandManager manager = ActiveManagers[i];
            if (manager == null)
            {
                ActiveManagers.RemoveAt(i);
                continue;
            }

            if (manager.OwnsCardRect(cardRect))
            {
                return manager;
            }
        }

        return null;
    }

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
        UpdateHandCardPositions(false);
    }

    private void UpdateHandCardPositions(bool animatePositions)
    {
        if (handContainer == null)
        {
            hoveredCardRect = null;
            StopHandLayoutAnimation();
            return;
        }

        SortHandCardsByCharacterOrder();
        List<RectTransform> handCardRects = GetHandCardRectsInHandOrder();

        int cardCount = handCardRects.Count;
        if (cardCount == 0)
        {
            hoveredCardRect = null;
            StopHandLayoutAnimation();
            return;
        }

        int hoveredIndex = GetHoveredCardIndex(handCardRects);
        if (hoveredIndex < 0)
        {
            hoveredCardRect = null;
        }

        bool shouldAnimate = animatePositions && hoverSpreadDuration > 0f && Application.isPlaying;
        List<Vector2> targetPositions = shouldAnimate ? new List<Vector2>(cardCount) : null;
        List<Quaternion> targetRotations = shouldAnimate ? new List<Quaternion>(cardCount) : null;

        if (!shouldAnimate)
        {
            StopHandLayoutAnimation();
        }

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

            x += GetHoverSpreadOffset(i, hoveredIndex);

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.SetSiblingIndex(i);

            Vector2 targetPosition = new Vector2(x, y);
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotationZ);

            if (shouldAnimate)
            {
                targetPositions.Add(targetPosition);
                targetRotations.Add(targetRotation);
            }
            else
            {
                cardRect.anchoredPosition = targetPosition;
                cardRect.localRotation = targetRotation;
            }
        }

        if (shouldAnimate)
        {
            StartHandLayoutAnimation(handCardRects, targetPositions, targetRotations);
        }
    }

    public void SetHoveredCard(RectTransform cardRect)
    {
        if (!OwnsCardRect(cardRect))
        {
            return;
        }

        if (hoveredCardRect == cardRect)
        {
            return;
        }

        hoveredCardRect = cardRect;
        UpdateHandCardPositions(true);
    }

    public void ClearHoveredCard(RectTransform cardRect, bool animatePositions = true)
    {
        if (hoveredCardRect != cardRect)
        {
            return;
        }

        hoveredCardRect = null;
        UpdateHandCardPositions(animatePositions);
    }

    private int GetHoveredCardIndex(List<RectTransform> handCardRects)
    {
        if (hoveredCardRect == null || handCardRects == null)
        {
            return -1;
        }

        return handCardRects.IndexOf(hoveredCardRect);
    }

    private bool OwnsCardRect(RectTransform cardRect)
    {
        return cardRect != null && handContainer != null && cardRect.parent == handContainer;
    }

    private float GetHoverSpreadOffset(int cardIndex, int hoveredIndex)
    {
        if (hoveredIndex < 0 || cardIndex == hoveredIndex)
        {
            return 0f;
        }

        int distance = Mathf.Abs(cardIndex - hoveredIndex);
        if (distance > 3)
        {
            return 0f;
        }

        float offset = distance switch
        {
            1 => hoverSpreadOffset,
            2 => secondHoverSpreadOffset,
            3 => thirdHoverSpreadOffset,
            _ => 0f
        };

        return cardIndex < hoveredIndex ? -offset : offset;
    }

    private void StartHandLayoutAnimation(
        List<RectTransform> cardRects,
        List<Vector2> targetPositions,
        List<Quaternion> targetRotations)
    {
        StopHandLayoutAnimation();
        handLayoutAnimation = StartCoroutine(AnimateHandLayout(cardRects, targetPositions, targetRotations));
    }

    private void StopHandLayoutAnimation()
    {
        if (handLayoutAnimation == null)
        {
            return;
        }

        StopCoroutine(handLayoutAnimation);
        handLayoutAnimation = null;
    }

    private System.Collections.IEnumerator AnimateHandLayout(
        List<RectTransform> cardRects,
        List<Vector2> targetPositions,
        List<Quaternion> targetRotations)
    {
        if (cardRects == null || targetPositions == null || targetRotations == null)
        {
            handLayoutAnimation = null;
            yield break;
        }

        int cardCount = Mathf.Min(cardRects.Count, Mathf.Min(targetPositions.Count, targetRotations.Count));
        List<Vector2> startPositions = new List<Vector2>(cardCount);
        List<Quaternion> startRotations = new List<Quaternion>(cardCount);

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform cardRect = cardRects[i];
            startPositions.Add(cardRect != null ? cardRect.anchoredPosition : Vector2.zero);
            startRotations.Add(cardRect != null ? cardRect.localRotation : Quaternion.identity);
        }

        float elapsedTime = 0f;
        float duration = Mathf.Max(0.01f, hoverSpreadDuration);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedTime / duration));

            for (int i = 0; i < cardCount; i++)
            {
                RectTransform cardRect = cardRects[i];
                if (cardRect == null)
                {
                    continue;
                }

                cardRect.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], t);
                cardRect.localRotation = Quaternion.Lerp(startRotations[i], targetRotations[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform cardRect = cardRects[i];
            if (cardRect == null)
            {
                continue;
            }

            cardRect.anchoredPosition = targetPositions[i];
            cardRect.localRotation = targetRotations[i];
        }

        handLayoutAnimation = null;
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
        hoveredCardRect = null;
        StopHandLayoutAnimation();

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
