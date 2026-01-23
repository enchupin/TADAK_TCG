using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전투 매니저 - UI 통합 버전
/// </summary>
public class BattleManager : MonoBehaviour {

    [Header("카드 데이터베이스")]
    public CardDatabase cardDatabase;

    // [Header("UI 시스템")]
    // public BattleUI battleUI;

    [Header("테스트 모드")]
    [Tooltip("체크하면 시작 시 자동으로 카드 테스트 실행")]
    public bool isTestMode = false;  // ← 기본값 false로 변경 (UI 모드)

    [Header("전투 데이터")]
    public PlayerData playerData;
    public Monster monster; // 임시 호출


    [Header("카드 데이터")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;

    private const int START_CARD_COUNT = 6;



    void Start() {
        InitializeBattle();
        InitializeUI();
        StartGame();
    }

    /// <summary>
    /// 전투 초기화
    /// </summary>
    void InitializeBattle() {






    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI() {


            /*
            if (battleUI != null) {
                battleUI.Initialize(this);
                battleUI.UpdateAllUI(playerData, monster);
            }
            */


    }




    /// <summary>
    /// 게임 시작 (UI 모드)
    /// </summary>
    void StartGame() {
        if (cardDatabase == null || cardDatabase.allCards.Count == 0) {
            Debug.LogError("CardDatabase가 없거나 카드가 로드되지 않았습니다!");
            return;
        }

        // 덱 초기화 및 셔플
        usableDeckManager.ShuffleDeck();

        // 시작 손패 뽑기
        DrawCards(START_CARD_COUNT);

        Debug.Log("게임 시작! 카드를 클릭해서 사용하세요.");
    }





    /// <summary>
    /// UsableDeckManager에서 카드를 드로우하여 손패에 추가
    /// </summary>
    public void DrawCards(int count) {
        if (usableDeckManager == null || handManager == null) {
            Debug.LogError("UsableDeckManager가 초기화되지 않았습니다!");
            return;
        }

        List<int> drawnCardIds = usableDeckManager.DrawCard(count);
        handManager.AddCardById(drawnCardIds);
    }


    /// <summary>
    /// 카드 사용 (UI에서 호출)
    /// </summary>
    public void PlayCard(int cardId) {

        if (playerData == null || monster == null) {
             Debug.LogError("PlayerData 또는 Monster가 초기화되지 않았습니다!");
             return;
        }

        // 에너지 체크
        if (playerData.energy < 0) {
            return;
        }


        // 손패에 있는지 확인 - HandManager가 관리하므로 UI에서 호출된 시점에서 이미 존재한다고 가정 가능
        // 하지만 안전을 위해 체크 로직을 유지하려면 HandManager를 통해 확인해야 함
        // 여기서는 간단히 패스 (HandManager에서 RemoveCard 실패시 처리 가능)

        // 에너지 소모


        // 카드 사용
        Debug.Log($"\n[플레이어] {cardId} 카드 사용!");
        
        // 현재 턴 카드 사용 횟수 전달
        // card.Play(playerData, monster, battleContext.cardsPlayedThisTurn);

        // 턴 상태 업데이트 (Card.Play에서 빠졌으므로 여기서 처리)
        // battleContext.cardsPlayedThisTurn++;
        // battleContext.cardsPlayedThisTurnList.Add(card);





        // 손패에서 제거 -> 버리기 더미로
        // 버리기 더미에 추가 (UsableDeckManager 이용)
        






        // UI 업데이트
        UpdateAllUI();

        // 전투 종료 체크
        CheckBattleEnd();
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn() {
        Debug.Log("\n=== 턴 종료 ===");

        // 손패를 버리기 더미로 (이전 주석 삭제)
        
        if (handManager != null && usableDeckManager != null)
        {
             // 현재 손패에 있는 모든 카드를 버리기 더미로 이동
             List<int> remainingCards = handManager.GetHandCardIds();
             usableDeckManager.AddToDiscard(remainingCards);
             
             // 손패 비우기
             handManager.ClearHand();
        }

        // 턴 카운터 초기화
        // battleContext.cardsPlayedThisTurn = 0;
        // battleContext.cardsPlayedThisTurnList.Clear();

        // 플레이어 턴 종료 처리 (방어력 리셋, 에너지 회복)
        playerData.OnTurnEnd();
        playerData.OnTurnStart();

        // UI 손패 클리어
        if (handManager != null)
            handManager.ClearHand();

        // 적 턴 (간단한 AI)
        monster.EnemyTurn(playerData);

        // 새 손패 뽑기
        DrawCards(5);

        UpdateAllUI();

        Debug.Log("새 턴 시작!");
    }


    /// <summary>
    /// 전투 종료 체크
    /// </summary>
    void CheckBattleEnd() {
        if (monster.IsDead()) {
            Debug.Log("\n🎉 승리! 적을 물리쳤습니다!");
            // 승리 UI 표시 (나중에 구현)
        } else if (playerData.IsDead()) {
            Debug.Log("\n💀 패배... 플레이어가 쓰러졌습니다.");
            // 패배 UI 표시 (나중에 구현)
        }
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    void UpdateAllUI() {

        // if (battleUI != null) battleUI.UpdateAllUI(playerData, monster);


    }





    // ========== 테스트 모드 (기존 코드) ==========
    /*
    void TestCards() {
        if (cardDatabase == null || cardDatabase.allCards.Count == 0) {
            Debug.LogError("CardDatabase가 없거나 카드가 로드되지 않았습니다!");
            return;
        }

        Debug.Log("\n=== 카드 테스트 시작 ===\n");

        Card fireball = cardDatabase.GetCardById(101010);
        if (fireball != null) {
            Debug.Log($"\n--- {fireball.cardName} 사용 ---");
            fireball.Play(playerData, monster, 0);
        }

        Card shield = cardDatabase.GetCardById(101020);
        if (shield != null) {
            Debug.Log($"\n--- {shield.cardName} 사용 ---");
            shield.Play(playerData, monster, 0);
        }

        Debug.Log("\n=== 카드 테스트 완료 ===");
    }
    */


}
