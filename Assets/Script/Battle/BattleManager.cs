using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전투 매니저 - UI 통합 버전 (싱글톤)
/// </summary>
public class BattleManager : MonoBehaviour {

    // 싱글톤 인스턴스
    public static BattleManager Instance { get; private set; }

    [Header("카드 데이터베이스")]
    public CardDatabase cardDatabase;

    // [Header("UI 시스템")]
    // public BattleUI battleUI;

    [Header("전투 데이터")]
    public PlayerData playerData;
    public Monster monster; // 임시 호출


    [Header("카드 데이터")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;

    public int drawCardCount = 6;



    void Awake() {
        // 싱글톤 초기화
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
    }

    void Start() {
        // 이벤트 구독
        CardGameEvents.OnCardClicked += HandleCardClicked;
        
        InitializeBattle();
        InitializeUI();
        StartGame();
    }

    void OnDestroy() {
        // 이벤트 구독 해제
        CardGameEvents.OnCardClicked -= HandleCardClicked;
    }

    /// <summary>
    /// 전투 초기화
    /// </summary>
    void InitializeBattle() {

        playerData = new PlayerData();

        // 임시 호출
        monster = new Monster();

        if (cardDatabase == null)
            return;

        usableDeckManager.InitializeDeck();
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
        if (usableDeckManager == null) {
            Debug.LogError("usableDeckManager가 없거나 카드가 로드되지 않았습니다!");
            return;
        }
        if (handManager == null) {
            Debug.LogError("handManager가 없거나 카드가 로드되지 않았습니다!");
            return;
        }
        if (playerData == null) {
            Debug.LogError("playerData가 없거나 카드가 로드되지 않았습니다!");
            return;
        }
        if (monster == null) {
            Debug.LogError("monster가 없거나 카드가 로드되지 않았습니다!");
            return;
        }


        // 덱 초기화 및 셔플
        usableDeckManager.ShuffleDeck();

        // 시작 손패 뽑기
        DrawCards(drawCardCount);

        Debug.Log("게임 시작! 카드를 클릭해서 사용하세요.");
    }





    /// <summary>
    /// UsableDeckManager에서 카드를 드로우하여 손패에 추가
    /// </summary>
    public void DrawCards(int count) {
        List<int> drawnCardIds = usableDeckManager.DrawCard(count);
        handManager.AddCardById(drawnCardIds);
    }


    /// <summary>
    /// 카드 클릭 이벤트 핸들러
    /// </summary>
    private void HandleCardClicked(CardClickedEventData eventData)
    {
        Debug.Log($"[BattleManager] 카드 클릭 이벤트 받음: {eventData.card.cardName}");
        PlayCard(eventData.cardUI);
    }

    /// <summary>
    /// 카드 사용
    /// </summary>
    private void PlayCard(CardUI playedCardUI) {
        Card playedCard = playedCardUI.card;
        int playedCardId = playedCard.cardId;

        // 에너지 소모
        playerData.energy -= playedCard.cost;
        Debug.Log($"\n[플레이어] {playedCard.cardName} 카드 사용! (에너지: {playerData.energy + playedCard.cost} → {playerData.energy})");

        // 카드 효과 실행
        playedCard.Play(this);

        // 손패에서 제거
        if (handManager != null) {
            handManager.RemoveCardFromHand(playedCardUI);
        }

        // UI 업데이트
        // UpdateAllUI();

        // 전투 종료 체크
        // CheckBattleEnd();
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
