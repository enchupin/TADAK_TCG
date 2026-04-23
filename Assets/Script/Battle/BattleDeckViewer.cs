using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 중 덱 버튼 클릭 시 현재 덱 카드를 패널에 표시
/// 카드 UI 생성과 레이아웃은 캐릭터북의 CardContainerManager를 재사용
/// </summary>
public class BattleDeckViewer : MonoBehaviour
{
    private enum DeckPanelViewType
    {
        None,
        DrawPile,
        DiscardPile,
        ExhaustPile
    }

    [Header("패널/컨테이너")]
    [SerializeField] private GameObject deckPanelRoot;
    [SerializeField] private CardContainerManager cardContainerManager;
    [SerializeField] private Button selectionConfirmButton;

    [Header("참조")]
    [SerializeField] private TrainingBattleManager battleManager;
    private bool isSelectionMode;
    private int requiredSelectionCount;
    private bool allowFewerSelection;
    private Action<List<Card>> onSelectionCompleted;
    private readonly List<Card> selectedCards = new List<Card>();
    private readonly List<CardController> selectedControllers = new List<CardController>();
    private DeckPanelViewType currentViewType;

    private void Awake() {
        ValidateRequiredReferences();
        deckPanelRoot.SetActive(false);
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// 덱 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickDeckButton() {
        if (isSelectionMode) {
            OnClickSelectionConfirm();
            return;
        }

        TogglePilePanel(DeckPanelViewType.DrawPile);
    }

    /// <summary>
    /// 버린 카드 더미 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickDiscardPileButton() {
        if (isSelectionMode) {
            OnClickSelectionConfirm();
            return;
        }

        TogglePilePanel(DeckPanelViewType.DiscardPile);
    }

    /// <summary>
    /// 소멸 카드 더미 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickExhaustPileButton() {
        if (isSelectionMode) {
            OnClickSelectionConfirm();
            return;
        }

        TogglePilePanel(DeckPanelViewType.ExhaustPile);
    }

    public void OnClickSelectionConfirm() {
        if (!isSelectionMode) {
            return;
        }

        if (!CanConfirmSelection()) {
            return;
        }

        CompleteSelection(new List<Card>(selectedCards));
    }

    private void TogglePilePanel(DeckPanelViewType viewType) {
        if (deckPanelRoot == null) {
            Debug.LogWarning("[BattleDeckViewer] deckPanelRoot 참조가 비어 있습니다");
            return;
        }

        if (deckPanelRoot.activeSelf && currentViewType == viewType) {
            deckPanelRoot.SetActive(false);
            currentViewType = DeckPanelViewType.None;
            return;
        }

        deckPanelRoot.SetActive(true);
        RefreshPileCards(viewType);
    }

    public bool OpenSelectionPanel(List<Card> selectableCards, int selectCount, Action<List<Card>> onComplete, bool allowFewer = false) {
        ValidateRequiredReferences();

        if (selectableCards == null) {
            return false;
        }

        deckPanelRoot.SetActive(true);

        isSelectionMode = true;
        requiredSelectionCount = Mathf.Max(0, selectCount);
        allowFewerSelection = allowFewer;
        onSelectionCompleted = onComplete;
        selectedCards.Clear();
        selectedControllers.Clear();
        UpdateConfirmButtonState();

        cardContainerManager.ClearHand();
        cardContainerManager.SetCardClickHandler(null);
        cardContainerManager.SetCardClickHandler(HandleSelectableCardClicked);
        cardContainerManager.AddCardWithoutInputController(selectableCards);
        UpdateConfirmButtonState();

        if (requiredSelectionCount == 0) {
            CompleteSelection(new List<Card>());
        }

        return true;
    }

    private void RefreshPileCards(DeckPanelViewType viewType) {
        ValidateRequiredReferences();

        if (battleManager.usableDeckManager == null) {
            throw new MissingReferenceException("[BattleDeckViewer] battleManager.usableDeckManager 참조가 비어 있습니다 TrainingBattleManager 연결을 확인하세요");
        }

        List<Card> cards = viewType switch
        {
            DeckPanelViewType.DrawPile => battleManager.usableDeckManager.GetDrawPile(),
            DeckPanelViewType.DiscardPile => battleManager.usableDeckManager.GetDiscardPile(),
            DeckPanelViewType.ExhaustPile => battleManager.usableDeckManager.GetExhaustPile(),
            _ => new List<Card>()
        };

        cardContainerManager.ClearHand();
        cardContainerManager.SetCardClickHandler(null);
        currentViewType = viewType;
        if (cards == null || cards.Count == 0) {
            return;
        }

        // 캐릭터북과 동일하게 입력 비활성 카드 UI 사용
        cardContainerManager.AddCardWithoutInputController(cards);
    }

    private void HandleSelectableCardClicked(CardController controller) {
        if (!isSelectionMode || controller == null || controller.Card == null) {
            return;
        }

        int selectedIndex = selectedControllers.IndexOf(controller);
        if (selectedIndex >= 0) {
            selectedControllers.RemoveAt(selectedIndex);
            selectedCards.RemoveAt(selectedIndex);
            SetSelectedVisual(controller, false);
            UpdateConfirmButtonState();
            return;
        }

        if (selectedCards.Count >= requiredSelectionCount) {
            return;
        }

        selectedControllers.Add(controller);
        selectedCards.Add(controller.Card);
        SetSelectedVisual(controller, true);
        UpdateConfirmButtonState();
    }

    private void CompleteSelection(List<Card> result) {
        Action<List<Card>> callback = onSelectionCompleted;

        isSelectionMode = false;
        requiredSelectionCount = 0;
        allowFewerSelection = false;
        onSelectionCompleted = null;
        selectedCards.Clear();
        selectedControllers.Clear();
        UpdateConfirmButtonState();

        cardContainerManager.SetCardClickHandler(null);
        cardContainerManager.ClearHand();
        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(false);
        }
        currentViewType = DeckPanelViewType.None;

        callback?.Invoke(result ?? new List<Card>());
    }

    private static void SetSelectedVisual(CardController controller, bool isSelected) {
        if (controller == null) {
            return;
        }

        Image image = controller.GetComponent<Image>();
        if (image == null) {
            return;
        }

        image.color = isSelected
            ? new Color(0.65f, 1f, 0.65f, 1f)
            : Color.white;
    }

    private bool CanConfirmSelection() {
        if (allowFewerSelection) {
            return selectedCards.Count <= requiredSelectionCount;
        }

        return selectedCards.Count >= requiredSelectionCount;
    }

    private void UpdateConfirmButtonState() {
        if (selectionConfirmButton == null) {
            return;
        }

        selectionConfirmButton.gameObject.SetActive(isSelectionMode);
        selectionConfirmButton.interactable = isSelectionMode && CanConfirmSelection();
    }

    private void ValidateRequiredReferences() {
        if (deckPanelRoot == null) {
            throw new MissingReferenceException("[BattleDeckViewer] deckPanelRoot 참조가 비어 있습니다");
        }
        if (cardContainerManager == null) {
            throw new MissingReferenceException("[BattleDeckViewer] cardContainerManager 참조가 비어 있습니다");
        }
        if (battleManager == null) {
            throw new MissingReferenceException("[BattleDeckViewer] battleManager 참조가 비어 있습니다");
        }
    }
}
