using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 손패에서 카드를 고르는 필수 선택 (패널 없이 전투 화면에서 진행)
/// 후보 카드만 누를 수 있고, 고른 카드는 화면 중앙에 나란히 놓이며 확인 버튼으로 완료
/// </summary>
public class HandCardSelectionController : MonoBehaviour
{
    private const float RowSpacing = 20f;

    private HandManager handManager;
    private Button confirmButton;
    private Transform centerAnchor;
    private RectTransform selectionRow;

    private Action<List<Card>> onCompleted;
    private int requiredCount;
    private bool allowFewer;
    private readonly List<CardController> selectedControllers = new List<CardController>();
    private readonly List<HandCardSelectionClick> clickReceivers = new List<HandCardSelectionClick>();
    private readonly List<Card> candidateCards = new List<Card>();
    // 후보가 아닌 손패의 원래 입력 상태 (선택이 끝나면 복구)
    private readonly List<(CanvasGroup group, bool blocksRaycasts)> blockedCards = new List<(CanvasGroup, bool)>();

    public bool IsActive { get; private set; }

    // 손패 표시용: 후보는 색 유지, 나머지는 낼 수 없는 카드처럼 어둡게
    public bool IsCandidate(Card card)
    {
        return card != null && candidateCards.Contains(card);
    }

    public void Configure(HandManager targetHandManager, Button targetConfirmButton, Transform targetCenterAnchor)
    {
        handManager = targetHandManager;
        centerAnchor = targetCenterAnchor;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnClickConfirm);
        }

        confirmButton = targetConfirmButton;
        if (confirmButton != null)
        {
            // 패널 아래에 배치돼 있어도 패널 없이 보이도록 같은 위치를 유지한 채 루트 캔버스로 옮김
            Canvas canvas = confirmButton.GetComponentInParent<Canvas>(true);
            if (canvas != null && confirmButton.transform.parent != canvas.rootCanvas.transform)
            {
                confirmButton.transform.SetParent(canvas.rootCanvas.transform, true);
            }

            confirmButton.onClick.RemoveListener(OnClickConfirm);
            confirmButton.onClick.AddListener(OnClickConfirm);
            confirmButton.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += RefreshConfirmLabel;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= RefreshConfirmLabel;
        Cancel();
    }

    // 모든 후보가 손패에 표시된 카드일 때만 손패 선택으로 진행 (아니면 목록 선택)
    public bool CanSelectFromHand(List<Card> cards)
    {
        if (handManager == null || handManager.HandContainer == null || cards == null || cards.Count == 0)
        {
            return false;
        }

        List<Card> handCards = handManager.GetHandCards();
        foreach (Card card in cards)
        {
            if (card == null || !handCards.Contains(card) || handManager.GetCardUI(card) == null)
            {
                return false;
            }
        }

        return true;
    }

    public bool Open(List<Card> candidates, int selectCount, bool allowFewerSelection, Action<List<Card>> onComplete)
    {
        if (IsActive || !CanSelectFromHand(candidates))
        {
            return false;
        }

        requiredCount = Mathf.Max(0, selectCount);
        if (requiredCount == 0)
        {
            onComplete?.Invoke(new List<Card>());
            return true;
        }

        IsActive = true;
        allowFewer = allowFewerSelection;
        onCompleted = onComplete;
        selectedControllers.Clear();
        candidateCards.Clear();
        candidateCards.AddRange(candidates);

        foreach (Transform child in handManager.HandContainer)
        {
            CardController controller = child.GetComponent<CardController>();
            if (controller == null || controller.Card == null)
            {
                continue;
            }

            if (candidates.Contains(controller.Card))
            {
                HandCardSelectionClick receiver = child.gameObject.AddComponent<HandCardSelectionClick>();
                receiver.Bind(controller, HandleCardClicked);
                clickReceivers.Add(receiver);
            }
            else
            {
                BlockCardInput(child.gameObject);
            }
        }

        // 손패 선택: 패널 없는 필수 선택 (ESC로 닫히지 않음, 전투 조작 · 툴팁 정지)
        // 등록 시 손패 표시가 다시 계산되어 후보 외 카드가 어두워짐
        UIModalStack.Register(new UIModalEntry(this, UIModalKind.Required, null));
        RefreshConfirmButton();
        return true;
    }

    private void HandleCardClicked(CardController controller)
    {
        if (!IsActive || controller == null)
        {
            return;
        }

        if (selectedControllers.Remove(controller))
        {
            ReturnToHand(controller);
        }
        else
        {
            // 이미 n장을 골랐으면 마지막(n번째)에 고른 카드를 손으로 돌려보내고 교체
            if (selectedControllers.Count >= requiredCount)
            {
                CardController replaced = selectedControllers[selectedControllers.Count - 1];
                selectedControllers.RemoveAt(selectedControllers.Count - 1);
                ReturnToHand(replaced);
            }

            selectedControllers.Add(controller);
            MoveToRow(controller);
        }

        LayoutRow();
        RefreshConfirmButton();
        SettingsManager.PlayPanelToggleSound();
    }

    private void OnClickConfirm()
    {
        if (!IsActive || !CanConfirm())
        {
            return;
        }

        List<Card> result = new List<Card>();
        foreach (CardController controller in selectedControllers)
        {
            if (controller != null && controller.Card != null)
            {
                result.Add(controller.Card);
            }
        }

        Action<List<Card>> callback = onCompleted;
        Restore();
        callback?.Invoke(result);
    }

    // 결과 없이 원래 상태로 되돌림 (전투 재시작 · 씬 이탈)
    public void Cancel()
    {
        if (IsActive)
        {
            Restore();
        }
    }

    private bool CanConfirm()
    {
        return allowFewer
            ? selectedControllers.Count <= requiredCount
            : selectedControllers.Count >= requiredCount;
    }

    // 고른 카드를 손패로 되돌리고 표시 · 클릭 수신기 · 패널 등록을 정리한 뒤 효과에 결과 전달
    private void Restore()
    {
        IsActive = false;
        foreach (CardController controller in selectedControllers)
        {
            ReturnToHand(controller, false);
        }
        selectedControllers.Clear();

        foreach (HandCardSelectionClick receiver in clickReceivers)
        {
            if (receiver != null)
            {
                Destroy(receiver);
            }
        }
        clickReceivers.Clear();

        foreach ((CanvasGroup group, bool blocksRaycasts) in blockedCards)
        {
            if (group != null)
            {
                group.blocksRaycasts = blocksRaycasts;
            }
        }
        blockedCards.Clear();
        candidateCards.Clear();

        onCompleted = null;
        handManager?.UpdateHandCardPositions();
        RefreshConfirmButton();
        UIModalStack.Unregister(this);
    }

    private void BlockCardInput(GameObject cardObject)
    {
        CanvasGroup group = cardObject.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = cardObject.AddComponent<CanvasGroup>();
        }

        blockedCards.Add((group, group.blocksRaycasts));
        group.blocksRaycasts = false;
    }

    private void MoveToRow(CardController controller)
    {
        RectTransform row = EnsureSelectionRow();
        if (row == null)
        {
            return;
        }

        // 손패 호버 상태(확대 · 앞으로 가져오기)를 먼저 풀고 옮김
        CardInteractionHandler handler = controller.interactionHandler;
        if (handler != null)
        {
            handler.OnPointerExit(null);
            handler.ResetHoverState();
            handler.enabled = false;
        }

        controller.transform.SetParent(row, true);
        controller.transform.localRotation = Quaternion.identity;
        // 중앙에는 호버 확대 크기로 표시 (프리팹 기본 크기는 작음)
        if (handler != null)
        {
            controller.transform.localScale = handler.HoverTargetScale;
        }
        handManager.UpdateHandCardPositions();
    }

    private void ReturnToHand(CardController controller, bool relayout = true)
    {
        if (controller == null || handManager == null || handManager.HandContainer == null)
        {
            return;
        }

        controller.transform.SetParent(handManager.HandContainer, true);
        if (controller.interactionHandler != null)
        {
            controller.interactionHandler.enabled = true;
            controller.interactionHandler.ResetHoverState();
        }

        if (relayout)
        {
            handManager.UpdateHandCardPositions();
        }
    }

    // 고른 카드를 중앙 위치 기준으로 가로로 나란히 배치
    private void LayoutRow()
    {
        if (selectionRow == null)
        {
            return;
        }

        // 해상도가 바뀌어도 중앙 위치를 따라감
        if (centerAnchor != null)
        {
            selectionRow.position = centerAnchor.position;
        }
        PlaceBelowPanels(selectionRow);

        float totalWidth = 0f;
        List<RectTransform> cards = new List<RectTransform>();
        foreach (CardController controller in selectedControllers)
        {
            RectTransform cardRect = controller != null ? controller.transform as RectTransform : null;
            if (cardRect == null)
            {
                continue;
            }

            cards.Add(cardRect);
            totalWidth += cardRect.rect.width * cardRect.localScale.x;
        }

        totalWidth += RowSpacing * Mathf.Max(0, cards.Count - 1);
        float x = -totalWidth * 0.5f;
        foreach (RectTransform cardRect in cards)
        {
            float width = cardRect.rect.width * cardRect.localScale.x;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(x + width * 0.5f, 0f);
            x += width + RowSpacing;
        }
    }

    private RectTransform EnsureSelectionRow()
    {
        if (selectionRow != null)
        {
            return selectionRow;
        }

        Canvas canvas = handManager != null && handManager.HandContainer != null
            ? handManager.HandContainer.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null)
        {
            Debug.LogWarning("[HandCardSelection] 손패 캔버스를 찾지 못해 선택한 카드를 중앙에 놓을 수 없습니다");
            return null;
        }

        GameObject rowObject = new GameObject("HandSelectionRow", typeof(RectTransform));
        selectionRow = (RectTransform)rowObject.transform;
        selectionRow.SetParent(canvas.rootCanvas.transform, false);
        selectionRow.sizeDelta = Vector2.zero;
        if (centerAnchor == null)
        {
            Debug.LogWarning("[HandCardSelection] 중앙 위치가 없어 캔버스 중앙에 놓습니다");
        }
        return selectionRow;
    }

    // 선택 화면은 전투 화면의 일부이므로 전투 UI 위, 덱 · 더미 등 패널(과 차단막) 아래에 그림
    private static void PlaceBelowPanels(Transform target)
    {
        Transform parent = target != null ? target.parent : null;
        if (parent == null)
        {
            return;
        }

        target.SetAsLastSibling();
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);
            if (sibling == target)
            {
                continue;
            }

            if (sibling.GetComponent<UIModalPanel>() != null || sibling.GetComponent<UIModalDimmer>() != null)
            {
                target.SetSiblingIndex(i);
                return;
            }
        }
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.gameObject.SetActive(IsActive);
        if (!IsActive)
        {
            return;
        }

        PlaceBelowPanels(confirmButton.transform);
        confirmButton.interactable = CanConfirm();
        RefreshConfirmLabel();
    }

    private void RefreshConfirmLabel()
    {
        if (confirmButton == null)
        {
            return;
        }

        TMP_Text label = confirmButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = GameText.Ui("ui.common.confirm");
        }
    }
}
