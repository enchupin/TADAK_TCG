using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 중 덱 버튼 클릭 시 현재 덱 카드를 패널에 표시
/// 카드 UI 생성과 레이아웃은 캐릭터북의 CardContainerManager를 재사용
/// </summary>
public class BattleDeckViewer : MonoBehaviour
{
    [Header("패널/컨테이너")]
    [SerializeField] private GameObject deckPanelRoot;
    [SerializeField] private CardContainerManager cardContainerManager;

    [Header("참조")]
    [SerializeField] private TrainingBattleManager battleManager;

    private void Awake() {
        ValidateRequiredReferences();
    }

    /// <summary>
    /// 덱 버튼 OnClick에 연결해서 사용
    /// </summary>
    public void OnClickDeckButton() {
        ToggleDeckPanel();
    }

    public void ToggleDeckPanel() {
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
        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(true);
        }
        RefreshDeckCards();
    }

    public void CloseDeckPanel() {
        if (deckPanelRoot != null) {
            deckPanelRoot.SetActive(false);
        }
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
        if (drawPileCards == null || drawPileCards.Count == 0) {
            return;
        }

        // 캐릭터북과 동일하게 입력 비활성 카드 UI 사용
        cardContainerManager.AddCardWithoutInputController(drawPileCards);
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
