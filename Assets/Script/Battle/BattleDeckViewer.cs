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

    private void Awake() {
        ValidateRequiredReferences();
    }

    /// <summary>
    /// 덱 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickDeckButton() {
        if (isSelectionMode) {
            OnClickSelectionConfirm();
            return;
        }

        ToggleDeckPanel();
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

    public void ToggleDeckPanel() {
        if (isSelectionMode) {
            Debug.Log("[BattleDeckViewer] 카드 선택 중에는 덱 뷰를 토글할 수 없습니다");
            return;
        }
        if (deckPanelRoot == null) {
            Debug.LogWarning("[BattleDeckViewer] deckPanelRoot 참조가 비어 있습니다");
            return;
        }

        bool willOpen = !deckPanelRoot.activeSelf;
        deckPanelRoot.SetActive(willOpen);

        if (willOpen) {
            RefreshDeckCards();
        }
    }

    public void OpenDeckPanel() {
        if (isSelectionMode) {
            Debug.Log("[BattleDeckViewer] 카드 선택 중에는 덱 뷰를 열 수 없습니다");
            return;
        }

        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(true);
        }
        RefreshDeckCards();
    }

    public void CloseDeckPanel() {
        if (isSelectionMode) {
            Debug.Log("[BattleDeckViewer] 카드 선택 중에는 패널을 닫을 수 없습니다");
            return;
        }

        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(false);
        }
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

    /// <summary>
    /// 현재 덱 상태를 스크롤뷰 카드 목록으로 다시 그림
    /// </summary>
    public void RefreshDeckCards() {
        ValidateRequiredReferences();

        if (battleManager.usableDeckManager == null) {
            throw new MissingReferenceException("[BattleDeckViewer] battleManager.usableDeckManager 참조가 비어 있습니다 TrainingBattleManager 연결을 확인하세요");
        }

        List<Card> drawPileCards = battleManager.usableDeckManager.GetDrawPile();

        // 기존 UI를 비우고 현재 덱 카드로 다시 채움
        cardContainerManager.ClearHand();
        cardContainerManager.SetCardClickHandler(null);
        if (drawPileCards == null || drawPileCards.Count == 0) {
            return;
        }

        // 캐릭터북과 동일하게 입력 비활성 카드 UI 사용
        cardContainerManager.AddCardWithoutInputController(drawPileCards);
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
