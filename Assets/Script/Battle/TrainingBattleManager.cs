using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 훈련 모드 전투 매니저 (싱글톤)
/// 3명의 캐릭터를 선택하여 하나의 플레이어로 플레이하는 훈련 모드 전용
/// </summary>
public class TrainingBattleManager : MonoBehaviour {

    // 싱글톤 인스턴스
    public static TrainingBattleManager Instance { get; private set; }

    [Header("UI 시스템")]
    public BattleUI battleUI;

    [Header("전투 데이터")]
    public PlayerData playerData;
    public Monster monster;
    public BattleContext battleContext;  // 수식 평가용 컨텍스트


    [Header("카드 데이터")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;

    // 런 동안 유지되는 영구 덱 (씬이 바뀌어도 유지되도록 static)
    [Header("덱 시스템")]
    public static BuildingDeck buildingDeck;


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
        CardPlayEvents.OnCardPlayed += HandleCardClicked;
        
        // 캐릭터 선택 초기화
        InitializeCharacterSelection();
        
        // CardManager는 자동으로 초기화됨 (RuntimeInitializeOnLoadMethod)
        
        InitializeBattle();
        InitializeUI();
        StartGame();
    }

    void OnDestroy() {
        // 이벤트 구독 해제
        CardPlayEvents.OnCardPlayed -= HandleCardClicked;
    }
    
    /// <summary>
    /// 캐릭터 선택 초기화
    /// </summary>
    void InitializeCharacterSelection()
    {
        // 선택된 캐릭터가 없으면 기본값 설정
        if (SelectedButtonControl.selectedCharacterList == null ||
            SelectedButtonControl.selectedCharacterList.Count != 3)
        {
            Debug.LogWarning("[BattleManager] 캐릭터 선택이 없습니다. 기본값으로 설정합니다.");
            
            if (SelectedButtonControl.selectedCharacterList == null)
            {
                SelectedButtonControl.selectedCharacterList = new System.Collections.Generic.List<Character>();
            }
            
            SelectedButtonControl.selectedCharacterList.Clear();
            
            // 테스트용: Isla, Ignia, Declan 사용
            SelectedButtonControl.selectedCharacterList.Add(Character.Isla);
            SelectedButtonControl.selectedCharacterList.Add(Character.Ignia);
            SelectedButtonControl.selectedCharacterList.Add(Character.Polar);
            
            Debug.Log($"[BattleManager] 기본 캐릭터 설정 완료: {SelectedButtonControl.selectedCharacterList.Count}명");
        }
    }

    /// <summary>
    /// 전투 초기화
    /// </summary>
    void InitializeBattle() {

        playerData = new PlayerData();
        monster = new Monster();
        battleContext = new BattleContext();  // 컨텍스트 초기화

        if (!CardManager.IsInitialized())
        {
            Debug.LogError("[BattleManager] CardManager가 초기화되지 않았습니다!");
            return;
        }

        // BuffManager 초기화 확인
        if (BuffManager.Instance == null)
        {
            Debug.Log("[BattleManager] BuffManager가 없어 새로 생성합니다.");
            GameObject go = new GameObject("BuffManager");
            go.AddComponent<BuffManager>();
        }

        // 1. BuildingDeck 초기화 (게임 최초 실행 시 한 번만)
        if (buildingDeck == null)
        {
            Debug.Log("[BattleManager] 새로운 Run 시작: BuildingDeck을 생성합니다.");
            buildingDeck = new BuildingDeck();
            buildingDeck.Initialize(SelectedButtonControl.selectedCharacterList);
        }
        else
        {
             Debug.Log($"[BattleManager] 기존 Run 이어하기: BuildingDeck 유지됨 ({buildingDeck.CopyDeck().Count}장)");
        }

        // 2. 전투용 덱(UsableDeck) 설정 - 영구 덱에서 복사
        List<Card> battleDeck = buildingDeck.CopyDeck();
        usableDeckManager.SetDeck(battleDeck);

        UpdateAllUI();
    }

    /// <summary>
    /// UI 초기화
    /// </summary>
    void InitializeUI() {
        if (battleUI != null)
        {
            battleUI.Initialize();
            battleUI.UpdateAllUI();
        }
    }





    void StartGame() {
        // 전투 시작
        battleContext.OnCombatStart();
        if (!CardManager.IsInitialized()) {
            Debug.LogError("[BattleManager] CardManager가 초기화되지 않았습니다!");
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
        List<Card> drawnCards = usableDeckManager.DrawCard(count);
        handManager.AddCard(drawnCards);
    }


    /// <summary>
    /// 카드 클릭 이벤트 핸들러
    /// </summary>
    private void HandleCardClicked(CardPlayEventData eventData)
    {
        Debug.Log($"[BattleManager] 카드 클릭 이벤트 받음: {eventData}");
        PlayCard(eventData.cardController);
    }

    /// <summary>
    /// 카드 사용
    /// </summary>
    private void PlayCard(CardController controller) {
        if (controller == null || controller.Card == null) {
            Debug.LogWarning("[TrainingBattleManager] CardController 또는 Card를 찾을 수 없습니다!");
            return;
        }
        Card playedCard = controller.Card;

        // 에너지 소모
        playerData.energy -= playedCard.cost;
        Debug.Log($"\n[플레이어] {playedCard.cardName} 카드 사용! (에너지: {playerData.energy + playedCard.cost} → {playerData.energy})");

        // 컨텍스트 업데이트
        battleContext.OnCardPlayed(playedCard);

        // 카드 효과 실행
        playedCard.Play(this);

        // 손패에서 제거
        if (handManager != null) {
            handManager.RemoveCardFromHand(controller.cardUI);
        }
        
        // 사용한 카드는 버리기 더미로 이동 (일회용 카드가 아니라면)
        // TODO: 소멸(Exhaust) 키워드 구현 시 수정 필요
        if (usableDeckManager != null)
        {
            usableDeckManager.AddToDiscard(playedCard);
        }
        
        UpdateAllUI();

        // 전투 종료 체크
        // CheckBattleEnd();
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn() {
        Debug.Log("\n=== 턴 종료 ===");


        // 손패 비우기
        if (handManager != null && usableDeckManager != null)
        {
             // 현재 손패에 있는 모든 카드를 버리기 더미로 이동
             List<Card> remainingCards = handManager.GetHandCards();
             usableDeckManager.AddToDiscard(remainingCards);
             
             // 손패 비우기
             handManager.ClearHand();
        }

        // 턴 카운터 초기화
        battleContext.OnTurnStart();

        // 플레이어 턴 종료 처리 (방어력 리셋, 에너지 회복)
        playerData.OnTurnEnd();
        playerData.OnTurnStart();

        // 적 턴 (간단한 AI)
        monster.EnemyTurn(playerData);

        // 새 손패 뽑기
        DrawCards(drawCardCount);

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
    public void UpdateAllUI() {
        if (battleUI != null) battleUI.UpdateAllUI();
    }


}
