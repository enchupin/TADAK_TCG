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
    // 선택 중 덱 · 더미 버튼으로 같은 패널을 보기로 잠시 바꾼 상태 (닫으면 선택 목록 · 고른 상태 복원)
    private bool isViewingDuringSelection;
    private readonly List<Card> selectionCandidates = new List<Card>();
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

        // 카드 목록 선택 중에도 열고 닫을 수 있는 전투 화면 버튼 (지도 버튼은 지도가 꺼져 있어도 찾도록 여기서 등록)
        UIBattleSpaceButtons.Register(drawPileButton);
        UIBattleSpaceButtons.Register(discardPileButton);
        UIBattleSpaceButtons.Register(exhaustPileButton);
        UIBattleSpaceButtons.Register(fullDeckButton);
        TrainingMapController map = FindAnyObjectByType<TrainingMapController>(FindObjectsInactive.Include);
        if (map != null) {
            UIBattleSpaceButtons.Register(UIButtonLookup.FindByOnClick(map, nameof(TrainingMapController.OpenMapPreview)));
        }

        if (selectionHideButton != null) {
            selectionHideButton.onClick.RemoveListener(ToggleSelectionHidden);
            selectionHideButton.onClick.AddListener(ToggleSelectionHidden);
            deckModal.KeepVisibleWhenHidden(selectionHideButton.gameObject);
        }
    }

    private void ToggleSelectionHidden() {
        if (!isSelectionMode || isViewingDuringSelection || deckModal == null) return;
        deckModal.ToggleHidden();
        SettingsManager.PlayPanelToggleSound();
    }

    private void ClosePilePanelFromEscape() {
        if (isSelectionMode) {
            // 선택 중 열어 본 덱 · 더미 보기를 닫고 선택 목록으로 복귀
            if (isViewingDuringSelection) {
                ShowSelectionContent();
                SettingsManager.PlayPanelToggleSound();
            }
            return;
        }
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
        isViewingDuringSelection = false;
        selectionCandidates.Clear();
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
        TogglePilePanel(DeckPanelViewType.DrawPile);
    }

    // 상단 덱 버튼은 전투 중 카드 위치와 관계없이 이번 게임의 전체 덱을 표시
    public void OnClickFullDeckButton() {
        if (TrainingBattleManager.buildingDeck == null) return;
        TogglePilePanel(DeckPanelViewType.FullDeck);
    }

    /// <summary>
    /// 버린 카드 더미 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickDiscardPileButton() {
        TogglePilePanel(DeckPanelViewType.DiscardPile);
    }

    /// <summary>
    /// 소멸 카드 더미 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickExhaustPileButton() {
        TogglePilePanel(DeckPanelViewType.ExhaustPile);
    }

    public void OnClickSelectionConfirm() {
        if (!isSelectionMode || isViewingDuringSelection) {
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
            // 선택 중 열어 본 보기는 닫는 대신 선택 목록으로 복귀
            if (isSelectionMode) {
                ShowSelectionContent();
                SettingsManager.PlayPanelToggleSound();
                return;
            }

            deckPanelRoot.SetActive(false);
            HideCardDetailPanel();
            currentViewType = DeckPanelViewType.None;
            SettingsManager.PlayPanelToggleSound();
            return;
        }

        // 선택 중이면 같은 패널을 보기로 잠시 바꿈 (숨겨 둔 상태여도 보기는 표시)
        if (isSelectionMode) {
            isViewingDuringSelection = true;
            deckModal.SetHidden(false);
        }

        // 보기 모드: ESC로 닫히고, 이 패널을 연 버튼만 차단막을 통과
        deckModal.SetKind(UIModalKind.Normal);
        Button openButton = ResolveOpenButton(viewType);
        deckModal.SetPassThrough(openButton != null ? (RectTransform)openButton.transform : null);
        deckPanelRoot.SetActive(true);
        RefreshPileCards(viewType);
        UpdateConfirmButtonState();
        SettingsManager.PlayPanelToggleSound();
    }

    public bool OpenSelectionPanel(List<Card> selectableCards, int selectCount, Action<List<Card>> onComplete, bool allowFewer = false) {
        ValidateRequiredReferences();

        if (selectableCards == null) {
            return false;
        }

        isSelectionMode = true;
        requiredSelectionCount = Mathf.Max(0, selectCount);
        allowFewerSelection = allowFewer;
        onSelectionCompleted = onComplete;
        selectionCandidates.Clear();
        selectionCandidates.AddRange(selectableCards);
        selectedCards.Clear();
        selectedControllers.Clear();
        deckModal.SetHidden(false);
        ShowSelectionContent();

        if (requiredSelectionCount == 0) {
            CompleteSelection(new List<Card>());
        }

        return true;
    }

    // 선택 목록 표시: 필수 패널 (ESC로 닫히지 않음, 숨김 버튼으로 잠시 숨김)
    // 선택은 전투 화면에 귀속되므로 상단 지도 · 덱 · 설정, 하단 더미 버튼은 차단막을 통과
    private void ShowSelectionContent() {
        isViewingDuringSelection = false;
        currentViewType = DeckPanelViewType.None;
        deckModal.SetKind(UIModalKind.Required);
        deckModal.SetPassThrough(UIBattleSpaceButtons.GetTargets());
        deckPanelRoot.SetActive(true);
        HideCardDetailPanel();

        cardContainerManager.ClearHand();
        cardContainerManager.SetCardClickHandler(HandleSelectableCardClicked);
        // 파괴 예약된 이전 카드가 남아 있으므로 새로 만든 카드는 뒤쪽 자식
        Transform container = cardContainerManager.CardContainer;
        int firstNewIndex = container != null ? container.childCount : 0;
        cardContainerManager.AddCardWithoutInputController(selectionCandidates);

        // 다시 그린 카드에 고른 상태 복원 (고른 순서 유지)
        selectedControllers.Clear();
        if (container != null) {
            foreach (Card selectedCard in selectedCards) {
                for (int i = firstNewIndex; i < container.childCount; i++) {
                    CardController controller = container.GetChild(i).GetComponent<CardController>();
                    if (controller != null && ReferenceEquals(controller.Card, selectedCard)) {
                        selectedControllers.Add(controller);
                        SetSelectedVisual(controller, true);
                        break;
                    }
                }
            }
        }
        // 찾지 못한 카드가 있으면 두 목록의 순서가 어긋나지 않게 다시 맞춤
        if (selectedControllers.Count != selectedCards.Count) {
            selectedCards.Clear();
            foreach (CardController controller in selectedControllers) {
                selectedCards.Add(controller.Card);
            }
        }
        UpdateConfirmButtonState();
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
        if ((isSelectionMode && !isViewingDuringSelection) || controller == null || controller.Card == null) {
            return;
        }

        OpenCardDetailPanel(controller);
    }

    private void CompleteSelection(List<Card> result) {
        Action<List<Card>> callback = onSelectionCompleted;

        isSelectionMode = false;
        isViewingDuringSelection = false;
        selectionCandidates.Clear();
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

        // 선택 중 보기로 바꾼 동안에는 확인 · 숨김 버튼을 감춤
        bool showSelectionButtons = isSelectionMode && !isViewingDuringSelection;
        selectionConfirmButton.gameObject.SetActive(showSelectionButtons);
        selectionConfirmButton.interactable = showSelectionButtons && CanConfirmSelection();
        if (selectionHideButton != null) {
            selectionHideButton.gameObject.SetActive(showSelectionButtons);
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
