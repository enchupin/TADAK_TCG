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
        ExhaustPile,
        FullDeck
    }

    [Header("패널/컨테이너")]
    [SerializeField] private GameObject deckPanelRoot;
    [SerializeField] private CardContainerManager cardContainerManager;
    [SerializeField] private Button selectionConfirmButton;
    [SerializeField] private GameObject cardDetailPanelRoot;
    [SerializeField] private Transform cardDetailCardParent;
    [SerializeField] private GameObject cardDetailCardPrefab;

    [Header("여는 버튼 (패널이 열려 있어도 차단막을 통과해 다시 눌러 닫음, 비우면 OnClick 연결로 찾음)")]
    [SerializeField] private Button drawPileButton;
    [SerializeField] private Button discardPileButton;
    [SerializeField] private Button exhaustPileButton;
    [SerializeField] private Button fullDeckButton;
    [Tooltip("카드 선택 중 패널을 잠시 숨기고 다시 펼치는 버튼 (패널 안에 배치, 비우면 숨김 기능 없음)")]
    [SerializeField] private Button selectionHideButton;

    [Header("참조")]
    [SerializeField] private TrainingBattleManager battleManager;
    private bool isSelectionMode;
    public bool IsSelectionActive => isSelectionMode;
    private int requiredSelectionCount;
    private bool allowFewerSelection;
    private Action<List<Card>> onSelectionCompleted;
    private readonly List<Card> selectedCards = new List<Card>();
    private readonly List<CardController> selectedControllers = new List<CardController>();
    private DeckPanelViewType currentViewType;
    private CardController cardDetailPreviewController;
    private CardUI cardDetailPreviewUI;
    private UIModalPanel deckModal;

    // 덱 목록 · 카드 확인 패널을 패널 스택에 등록 (ESC는 UIEscapeRouter가 맨 위 패널부터 닫음)
    private void RegisterModals() {
        deckModal = UIModalPanel.Ensure(deckPanelRoot, new UIModalPanel.Options {
            close = ClosePilePanelFromEscape
        });

        // 카드 확인 패널은 자체 배경이 전체 화면을 덮으므로 차단막 없이 등록
        if (cardDetailPanelRoot != null) {
            UIModalPanel.Ensure(cardDetailPanelRoot, new UIModalPanel.Options {
                useDimmer = false,
                close = CloseCardDetailPanel
            });
        }

        // 에디터의 빈 직렬화 참조는 가짜 null이라 ??= 대신 == 비교
        if (drawPileButton == null) drawPileButton = UIButtonLookup.FindByOnClick(this, nameof(OnClickDeckButton));
        if (discardPileButton == null) discardPileButton = UIButtonLookup.FindByOnClick(this, nameof(OnClickDiscardPileButton));
        if (exhaustPileButton == null) exhaustPileButton = UIButtonLookup.FindByOnClick(this, nameof(OnClickExhaustPileButton));
        if (fullDeckButton == null) fullDeckButton = UIButtonLookup.FindByOnClick(this, nameof(OnClickFullDeckButton));

        if (selectionHideButton != null) {
            selectionHideButton.onClick.RemoveListener(ToggleSelectionHidden);
            selectionHideButton.onClick.AddListener(ToggleSelectionHidden);
            deckModal.KeepVisibleWhenHidden(selectionHideButton.gameObject);
        }
    }

    private void ToggleSelectionHidden() {
        if (!isSelectionMode || deckModal == null) return;
        deckModal.ToggleHidden();
        SettingsManager.PlayPanelToggleSound();
    }

    private void ClosePilePanelFromEscape() {
        if (isSelectionMode) return;
        deckPanelRoot.SetActive(false);
        HideCardDetailPanel();
        currentViewType = DeckPanelViewType.None;
        SettingsManager.PlayPanelToggleSound();
    }

    private Button ResolveOpenButton(DeckPanelViewType viewType) {
        return viewType switch
        {
            DeckPanelViewType.DrawPile => drawPileButton,
            DeckPanelViewType.DiscardPile => discardPileButton,
            DeckPanelViewType.ExhaustPile => exhaustPileButton,
            DeckPanelViewType.FullDeck => fullDeckButton,
            _ => null
        };
    }

    public void ResetForCombat()
    {
        isSelectionMode = false;
        onSelectionCompleted = null;
        selectedCards.Clear();
        selectedControllers.Clear();
        requiredSelectionCount = 0;
        allowFewerSelection = false;
        currentViewType = DeckPanelViewType.None;
        if (cardContainerManager != null)
        {
            cardContainerManager.ClearHand();
            cardContainerManager.SetCardClickHandler(null);
        }
        if (deckPanelRoot != null) deckPanelRoot.SetActive(false);
        HideCardDetailPanel();
        UpdateConfirmButtonState();
    }
    private void Awake() {
        ValidateRequiredReferences();
        RegisterModals();
        deckPanelRoot.SetActive(false);
        HideCardDetailPanel();
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

    // 상단 덱 버튼은 전투 중 카드 위치와 관계없이 이번 게임의 전체 덱을 표시
    public void OnClickFullDeckButton() {
        if (isSelectionMode || TrainingBattleManager.buildingDeck == null) return;
        TogglePilePanel(DeckPanelViewType.FullDeck);
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
            HideCardDetailPanel();
            currentViewType = DeckPanelViewType.None;
            SettingsManager.PlayPanelToggleSound();
            return;
        }

        // 보기 모드: ESC로 닫히고, 이 패널을 연 버튼만 차단막을 통과
        deckModal.SetKind(UIModalKind.Normal);
        Button openButton = ResolveOpenButton(viewType);
        deckModal.SetPassThrough(openButton != null ? (RectTransform)openButton.transform : null);
        deckPanelRoot.SetActive(true);
        RefreshPileCards(viewType);
        SettingsManager.PlayPanelToggleSound();
    }

    public bool OpenSelectionPanel(List<Card> selectableCards, int selectCount, Action<List<Card>> onComplete, bool allowFewer = false) {
        ValidateRequiredReferences();

        if (selectableCards == null) {
            return false;
        }

        // 선택 모드: 필수 패널 (ESC 무시, 통과 버튼 없음, 숨김 버튼으로만 잠시 숨김)
        deckModal.SetKind(UIModalKind.Required);
        deckModal.SetPassThrough();
        deckModal.SetHidden(false);
        deckPanelRoot.SetActive(true);

        isSelectionMode = true;
        requiredSelectionCount = Mathf.Max(0, selectCount);
        allowFewerSelection = allowFewer;
        onSelectionCompleted = onComplete;
        selectedCards.Clear();
        selectedControllers.Clear();
        HideCardDetailPanel();
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
            DeckPanelViewType.FullDeck => TrainingBattleManager.buildingDeck.CopyDeck(),
            _ => new List<Card>()
        };

        if (viewType == DeckPanelViewType.DrawPile || viewType == DeckPanelViewType.FullDeck) {
            cards.Sort(CompareCardsById);
        }

        HideCardDetailPanel();
        cardContainerManager.ClearHand();
        cardContainerManager.SetCardClickHandler(HandleViewedCardClicked);
        currentViewType = viewType;
        if (cards == null || cards.Count == 0) {
            return;
        }

        // 캐릭터북과 동일하게 입력 비활성 카드 UI 사용
        cardContainerManager.AddCardWithoutInputController(cards);
    }

    private static int CompareCardsById(Card left, Card right) {
        if (ReferenceEquals(left, right)) {
            return 0;
        }

        if (left == null) {
            return 1;
        }

        if (right == null) {
            return -1;
        }

        int cardIdCompare = left.cardId.CompareTo(right.cardId);
        if (cardIdCompare != 0) {
            return cardIdCompare;
        }

        return string.Compare(left.cardName, right.cardName, StringComparison.Ordinal);
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

    private void HandleViewedCardClicked(CardController controller) {
        if (isSelectionMode || controller == null || controller.Card == null) {
            return;
        }

        OpenCardDetailPanel(controller);
    }

    private void CompleteSelection(List<Card> result) {
        Action<List<Card>> callback = onSelectionCompleted;

        isSelectionMode = false;
        requiredSelectionCount = 0;
        allowFewerSelection = false;
        onSelectionCompleted = null;
        selectedCards.Clear();
        selectedControllers.Clear();
        HideCardDetailPanel();
        UpdateConfirmButtonState();

        cardContainerManager.SetCardClickHandler(null);
        cardContainerManager.ClearHand();
        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(false);
        }
        currentViewType = DeckPanelViewType.None;

        callback?.Invoke(result ?? new List<Card>());
    }

    public void CloseCardDetailPanel() {
        bool wasVisible = cardDetailPanelRoot != null && cardDetailPanelRoot.activeInHierarchy;
        HideCardDetailPanel();
        if (wasVisible) SettingsManager.PlayPanelToggleSound();
    }

    private void HideCardDetailPanel() {
        ResolveCardDetailPreviewUI()?.HideBuffTooltip();
        if (cardDetailPanelRoot != null) {
            cardDetailPanelRoot.SetActive(false);
        }
    }

    private void OpenCardDetailPanel(CardController sourceController) {
        if (sourceController == null || sourceController.Card == null) {
            return;
        }

        if (!TryEnsureCardDetailPreview()) {
            return;
        }

        if (cardDetailPanelRoot != null) {
            cardDetailPanelRoot.SetActive(true);
            SettingsManager.PlayPanelToggleSound();
        }

        if (cardDetailPreviewController != null) {
            cardDetailPreviewController.Initialize(sourceController.Card);
            cardDetailPreviewController.isPlayable = true;
        } else if (cardDetailPreviewUI != null) {
            cardDetailPreviewUI.UpdateDisplay(sourceController.Card);
            cardDetailPreviewUI.SetPlayable(true);
        }

        CardUI detailPreviewUI = ResolveCardDetailPreviewUI();
        if (detailPreviewUI != null) {
            Canvas.ForceUpdateCanvases();
            detailPreviewUI.ShowBuffTooltip();
        }
    }

    private bool TryEnsureCardDetailPreview() {
        if (cardDetailPreviewController != null || cardDetailPreviewUI != null) {
            return true;
        }

        Transform previewParent = cardDetailCardParent != null ? cardDetailCardParent : cardDetailPanelRoot != null ? cardDetailPanelRoot.transform : null;
        if (previewParent == null) {
            Debug.LogWarning("[BattleDeckViewer] 카드 상세 패널 부모가 연결되지 않았습니다");
            return false;
        }

        if (cardDetailCardPrefab == null) {
            Debug.LogError("[BattleDeckViewer] 카드 상세 프리뷰 프리팹이 연결되지 않았습니다");
            return false;
        }

        GameObject detailCardObject = Instantiate(cardDetailCardPrefab, previewParent);
        cardDetailPreviewController = detailCardObject.GetComponent<CardController>();
        cardDetailPreviewUI = detailCardObject.GetComponent<CardUI>();

        RectTransform detailCardRect = detailCardObject.transform as RectTransform;
        if (detailCardRect != null) {
            detailCardRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailCardRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailCardRect.pivot = new Vector2(0.5f, 0.5f);
            detailCardRect.anchoredPosition = Vector2.zero;
            detailCardRect.localRotation = Quaternion.identity;
            detailCardRect.localScale = Vector3.one;
        }

        if (cardDetailPreviewController != null) {
            cardDetailPreviewController.useInteractionHandler = false;

            if (cardDetailPreviewController.interactionHandler != null) {
                cardDetailPreviewController.interactionHandler.showPlayThreshold = false;
                cardDetailPreviewController.interactionHandler.enabled = false;
            }
        }

        CardSelectionClickHandler clickHandler = detailCardObject.GetComponent<CardSelectionClickHandler>();
        if (clickHandler != null) {
            Destroy(clickHandler);
        }

        if (cardDetailPreviewController == null && cardDetailPreviewUI == null) {
            Debug.LogWarning("[BattleDeckViewer] 카드 상세 프리뷰 프리팹에 CardController 또는 CardUI가 없습니다");
            return false;
        }

        return true;
    }

    private CardUI ResolveCardDetailPreviewUI() {
        if (cardDetailPreviewUI != null) {
            return cardDetailPreviewUI;
        }

        if (cardDetailPreviewController != null) {
            return cardDetailPreviewController.cardUI;
        }

        return null;
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
        if (selectionHideButton != null) {
            selectionHideButton.gameObject.SetActive(isSelectionMode);
        }
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
