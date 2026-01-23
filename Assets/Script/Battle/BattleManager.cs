using UnityEngine;

/// <summary>
/// 전투 매니저 - UI 통합 버전
/// </summary>
public class BattleManager : MonoBehaviour {
    [Header("카드 데이터베이스")]
    public CardDatabase cardDatabase;

    [Header("UI 시스템")]
    public HandManager handManager;
    public BattleUI battleUI;

    [Header("테스트 모드")]
    [Tooltip("체크하면 시작 시 자동으로 카드 테스트 실행")]
    public bool isTestMode = false;  // ← 기본값 false로 변경 (UI 모드)

    [Header("전투 컨텍스트")]
    private BattleContext battleContext;

    void Start() {
        InitializeBattle();
        InitializeUI();

        if (isTestMode) {
            TestCards();  // 테스트 모드: 자동 실행
        } else {
            StartGame();  // 실제 게임: UI 모드
        }
    }

    /// <summary>
    /// 전투 초기화
    /// </summary>
    void InitializeBattle() {
        battleContext = new BattleContext();

        // 플레이어 초기 상태
        battleContext.playerHP = 80;
        battleContext.playerMaxHP = 80;
        battleContext.playerDefense = 0;
        battleContext.playerEnergy = 3;
        battleContext.playerStrength = 0;

        // 적 초기 상태
        battleContext.enemyHP = 50;
        battleContext.enemyMaxHP = 50;
        battleContext.enemyDefense = 0;

        Debug.Log("=== 전투 시작! ===");
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI() {
        if (handManager != null)
            handManager.Initialize(this);

        if (battleUI != null) {
            battleUI.Initialize(this);
            battleUI.UpdateAllUI(battleContext);
        }
    }

    /// <summary>
    /// 게임 시작 (UI 모드)
    /// </summary>
    void StartGame() {
        if (cardDatabase == null || cardDatabase.allCards.Count == 0) {
            Debug.LogError("CardDatabase가 없거나 카드가 로드되지 않았습니다!");
            return;
        }

        // 덱 초기화
        battleContext.deck.AddRange(cardDatabase.allCards);
        ShuffleDeck();

        // 시작 손패 뽑기
        DrawCards(5);

        Debug.Log("게임 시작! 카드를 클릭해서 사용하세요.");
    }

    /// <summary>
    /// 카드 뽑기
    /// </summary>
    void DrawCards(int count) {
        UsableDeckManager.Instance.DrawCard(count);

        // UI 업데이트
        if (handManager != null) {
            // 새로 뽑은 카드들을 UI에 추가
            int startIndex = battleContext.hand.Count - count;
            if (startIndex < 0) startIndex = 0;

            for (int i = startIndex; i < battleContext.hand.Count; i++) {
                handManager.AddCard(battleContext.hand[i]);
            }

            UpdateHandUI();
        }
    }

    /// <summary>
    /// 덱 섞기
    /// </summary>
    void ShuffleDeck() {
        for (int i = battleContext.deck.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = battleContext.deck[i];
            battleContext.deck[i] = battleContext.deck[randomIndex];
            battleContext.deck[randomIndex] = temp;
        }
    }

    /// <summary>
    /// 카드 사용 (UI에서 호출)
    /// </summary>
    public void PlayCard(Card card) {
        if (card == null || battleContext == null) {
            Debug.LogError("카드 또는 BattleContext가 null입니다!");
            return;
        }

        // 에너지 체크
        if (battleContext.playerEnergy < card.cost) {
            Debug.LogWarning($"에너지 부족! (필요: {card.cost}, 현재: {battleContext.playerEnergy})");
            return;
        }

        // 손패에 있는지 확인
        if (!battleContext.hand.Contains(card)) {
            Debug.LogWarning($"{card.cardName}이(가) 손패에 없습니다!");
            return;
        }

        // 에너지 소모
        battleContext.playerEnergy -= card.cost;

        // 카드 사용
        Debug.Log($"\n[플레이어] {card.cardName} 사용!");
        card.Play(battleContext);

        // 손패에서 제거 → 버리기 더미로
        battleContext.hand.Remove(card);
        battleContext.discardPile.Add(card);

        // UI 업데이트
        if (handManager != null)
            handManager.RemoveCard(card);

        UpdateAllUI();

        // 전투 종료 체크
        CheckBattleEnd();
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn() {
        Debug.Log("\n=== 턴 종료 ===");

        // 방어력 초기화
        battleContext.playerDefense = 0;

        // 손패를 버리기 더미로
        battleContext.discardPile.AddRange(battleContext.hand);
        battleContext.hand.Clear();

        // 턴 카운터 초기화
        battleContext.cardsPlayedThisTurn = 0;
        battleContext.cardsPlayedThisTurnList.Clear();

        // 에너지 회복
        battleContext.playerEnergy = 3;

        // UI 손패 클리어
        if (handManager != null)
            handManager.ClearHand();

        // 적 턴 (간단한 AI)
        EnemyTurn();

        // 새 손패 뽑기
        DrawCards(5);

        UpdateAllUI();

        Debug.Log("새 턴 시작!");
    }

    /// <summary>
    /// 적 턴 (간단한 AI)
    /// </summary>
    void EnemyTurn() {
        // 간단한 AI: 랜덤 데미지
        int damage = Random.Range(5, 15);
        int finalDamage = Mathf.Max(0, damage - battleContext.playerDefense);

        battleContext.playerHP -= finalDamage;
        battleContext.playerDefense = Mathf.Max(0, battleContext.playerDefense - damage);

        Debug.Log($"[적] 공격! 플레이어에게 {finalDamage} 데미지!");

        CheckBattleEnd();
    }

    /// <summary>
    /// 전투 종료 체크
    /// </summary>
    void CheckBattleEnd() {
        if (battleContext.enemyHP <= 0) {
            Debug.Log("\n🎉 승리! 적을 물리쳤습니다!");
            // 승리 UI 표시 (나중에 구현)
        } else if (battleContext.playerHP <= 0) {
            Debug.Log("\n💀 패배... 플레이어가 쓰러졌습니다.");
            // 패배 UI 표시 (나중에 구현)
        }
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    void UpdateAllUI() {
        if (battleUI != null)
            battleUI.UpdateAllUI(battleContext);

        UpdateHandUI();
    }

    /// <summary>
    /// 손패 UI 업데이트 (사용 가능 여부)
    /// </summary>
    void UpdateHandUI() {
        if (handManager != null)
            handManager.UpdatePlayableCards(battleContext.playerEnergy);
    }

    // ========== 테스트 모드 (기존 코드) ==========

    void TestCards() {
        if (cardDatabase == null || cardDatabase.allCards.Count == 0) {
            Debug.LogError("CardDatabase가 없거나 카드가 로드되지 않았습니다!");
            return;
        }

        Debug.Log("\n=== 카드 테스트 시작 ===\n");

        Card fireball = cardDatabase.GetCardById("CARD_001");
        if (fireball != null) {
            Debug.Log($"\n--- {fireball.cardName} 사용 ---");
            fireball.Play(battleContext);
        }

        Card shield = cardDatabase.GetCardById("CARD_002");
        if (shield != null) {
            Debug.Log($"\n--- {shield.cardName} 사용 ---");
            shield.Play(battleContext);
        }

        Debug.Log("\n=== 카드 테스트 완료 ===");
    }
}
