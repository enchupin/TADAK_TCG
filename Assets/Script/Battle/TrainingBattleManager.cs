using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 훈련 모드 전투 매니저 (싱글톤)
/// 3명의 캐릭터를 선택하여 하나의 플레이어로 플레이하는 훈련 모드 전용
/// </summary>
public class TrainingBattleManager : MonoBehaviour {

    // 싱글톤 인스턴스
    public static TrainingBattleManager Instance { get; private set; }

    [Header("전투 데이터")]
    public PlayerData playerData;
    public BattleContext battleContext;  // 수식 평가용 컨텍스트

    [Header("매니저 연결")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;
    public BattleUI battleUI;
    public MonsterSpawner monsterSpawner;

    [Header("드로우 수")]
    public int drawCardCount = 6;

    [Header("스폰된 몬스터")]
    public List<Monster> spawnedMonsters = new List<Monster>();

    // 런 동안 유지되는 영구 덱 (씬이 바뀌어도 유지되도록 static)
    [Header("덱 시스템")]
    public static BuildingDeck buildingDeck;

    public void RegisterMonster(Monster monster)
    {
        if (!spawnedMonsters.Contains(monster))
        {
            spawnedMonsters.Add(monster);
            Debug.Log($"[BattleManager] 몬스터 등록됨: {monster.name} (현재 총 {spawnedMonsters.Count}마리)");
        }
    }

    public void UnregisterMonster(Monster monster)
    {
        if (spawnedMonsters.Contains(monster))
        {
            spawnedMonsters.Remove(monster);
            Debug.Log($"[BattleManager] 몬스터 등록 해제됨: {monster.name} (현재 총 {spawnedMonsters.Count}마리)");
        }
    }



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
        
        InitializeBattle();           // PlayerData.Create() → Instance 등록
        battleUI?.UpdateAllUI();      // 데이터 준비 후 UI 갱신
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
        if (playerData == null) {
            playerData = PlayerData.Create();
        }

        // 선택된 캐릭터의 스탯 합산
        int totalMaxHp = 0;
        int totalDefense = 0;
        int totalMaxEnergy = 3;

        foreach (Character character in SelectedButtonControl.selectedCharacterList)
        {
            CharacterData data = CharacterManager.GetCharacterByEnum(character);
            if (data != null) {
                totalMaxHp += data.maxHp;
            }
            else {
                Debug.LogWarning($"[BattleManager] {character}의 CharacterData를 찾을 수 없습니다.");
            }
        }

        // PlayerData 초기화 메서드 호출
        playerData.Initialize(totalMaxHp, totalDefense, totalMaxEnergy);

        // 몬스터 스폰
        if (monsterSpawner != null) {
            monsterSpawner.SpawnMonster();
            // 스폰된 몬스터는 스스로 Start()에서 RegisterMonster()를 호출하여 매니저에 등록될 것입니다.
        }
        else {
            Debug.LogWarning("[BattleManager] MonsterSpawner가 할당되지 않아 몬스터를 스폰할 수 없습니다.");
        }

        battleContext = new BattleContext();  // 컨텍스트 초기화

        if (!CardManager.IsInitialized()) {
            Debug.LogError("[BattleManager] CardManager가 초기화되지 않았습니다!");
            return;
        }

        // BuffManager 초기화 확인
        if (BuffManager.Instance == null) {
            Debug.Log("[BattleManager] BuffManager가 없어 새로 생성합니다.");
            GameObject go = new GameObject("BuffManager");
            go.AddComponent<BuffManager>();
        }

        // 1. BuildingDeck 초기화 (게임 최초 실행 시 한 번만)
        if (buildingDeck == null) {
            Debug.Log("[BattleManager] 새로운 Run 시작: BuildingDeck을 생성합니다.");
            buildingDeck = new BuildingDeck();
            buildingDeck.Initialize(SelectedButtonControl.selectedCharacterList);
        }
        else {
             Debug.Log($"[BattleManager] 기존 Run 이어하기: BuildingDeck 유지됨 ({buildingDeck.CopyDeck().Count}장)");
        }

        // 2. 전투용 덱(UsableDeck) 설정 - 영구 덱에서 복사
        List<Card> battleDeck = buildingDeck.CopyDeck();
        usableDeckManager.SetDeck(battleDeck);

        UpdateAllUI();
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


        // 덱 초기화 및 셔플
        usableDeckManager.ShuffleDeck();

        if (isDebugMode) {
            DebugDrawSpecificEffectCard();
        } else {
            // 시작 손패 뽑기
            DrawCards(drawCardCount);
        }

        Debug.Log("게임 시작! 카드를 클릭해서 사용하세요.");
    }





    /// <summary>
    /// UsableDeckManager에서 카드를 드로우하여 손패에 추가
    /// </summary>
    public void DrawCards(int count) {
        List<Card> drawnCards = usableDeckManager.DrawCard(count);
        handManager.AddCard(drawnCards);
    }



    // 테스트 용
    // 디버그 모드 선택 시 특정 이펙트를 보유한 모든 카드가 핸드에 추가됨
    [Header("디버그 모드")]
    public bool isDebugMode = false;
    public EffectType debugTargetEffect = EffectType.Barrier;
    // 테스트 용
    [ContextMenu("디버그: 특정 효과 카드 뽑기")]
    public void DebugDrawSpecificEffectCard()
    {
        if (!isDebugMode || usableDeckManager == null || handManager == null) return;

        List<Card> matchingCards = new List<Card>();
        Queue<Card> remainingDeck = new Queue<Card>();

        while (usableDeckManager.usableDeck.Count > 0)
        {
            Card card = usableDeckManager.usableDeck.Dequeue();
            bool isMatch = false;

            if (card.effects != null)
            {
                foreach (var effect in card.effects)
                {
                    switch (debugTargetEffect)
                    {
                        case EffectType.Barrier:
                            isMatch = effect is BarrierEffect;
                            break;
                        case EffectType.Damage:
                            isMatch = effect is DamageEffect;
                            break;
                        case EffectType.Draw:
                            isMatch = effect is DrawEffect;
                            break;
                        case EffectType.Attack:
                            isMatch = effect is AttackEffect;
                            break;
                        case EffectType.Execute:
                            isMatch = effect is ExecuteDamageEffect;
                            break;
                        case EffectType.Heal:
                            isMatch = effect is HealEffect;
                            break;
                        case EffectType.Buff:
                            isMatch = effect is BuffEffect;
                            break;
                        default:
                            break;
                    }

                    if (isMatch) break; // 하나라도 맞으면 이 카드는 매치됨
                }
            }

            if (isMatch)
            {
                matchingCards.Add(card);
            }
            else
            {
                remainingDeck.Enqueue(card);
            }
        }

        // 매치되지 않은 카드들은 다시 덱으로
        usableDeckManager.usableDeck = remainingDeck;

        if (matchingCards.Count > 0)
        {
            Debug.Log($"[디버그] {debugTargetEffect} 효과를 가진 카드 {matchingCards.Count}장을 손패로 가져옵니다!");
            handManager.AddCard(matchingCards);
        }
        else
        {
            Debug.LogWarning($"[디버그] 덱에 {debugTargetEffect} 효과를 가진 카드가 없습니다!");
        }
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


        // 새 손패 뽑기
        DrawCards(drawCardCount);

        UpdateAllUI();

        Debug.Log("새 턴 시작!");
    }


    /// <summary>
    /// 전투 종료 체크
    /// </summary>
    void CheckBattleEnd() {
        /*
        if (monster.IsDead()) {
            Debug.Log("\n🎉 승리! 적을 물리쳤습니다!");
            // 승리 UI 표시 (나중에 구현)
        } else if (playerData.IsDead()) {
            Debug.Log("\n💀 패배... 플레이어가 쓰러졌습니다.");
            // 패배 UI 표시 (나중에 구현)
        }
        */
    }

    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    public void UpdateAllUI() {
        if (battleUI != null) battleUI.UpdateAllUI();
    }


}
