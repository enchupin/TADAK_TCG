using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum BattleTurnState
{
    None,
    CombatStart,
    PlayerTurnStart,
    PlayerAction,
    PlayerTurnEnd,
    EnemyTurnStart,
    EnemyAction,
    EnemyTurnEnd,
    CombatEnd
}

public enum DebugCardFilterType
{
    Effect,
    Keyword,
    Buff
}

public enum DebugCardKeyword
{
    Keep = CardKeywordIds.Keep,
    Unplayable = CardKeywordIds.Unplayable,
    Exhaust = CardKeywordIds.Exhaust,
    Power = CardKeywordIds.Power,
    Opening = CardKeywordIds.Opening,
    Shadow = CardKeywordIds.Shadow,
    Finale = CardKeywordIds.Finale,
    Ghost = CardKeywordIds.Ghost,
    Unique = CardKeywordIds.Unique
}

/// <summary>
/// Training mode battle orchestrator.
/// Responsible for wiring references and delegating combat flow to subsystems.
/// </summary>
public class TrainingBattleManager : MonoBehaviour
{
    private sealed class PendingMonsterRevive
    {
        public Monster monster;
        public int remainingTurns;
    }

    public static TrainingBattleManager Instance { get; private set; }

    [Header("Battle Data")]
    [HideInInspector] public PlayerData playerData;
    public BattleContext battleContext;

    [Header("Manager References")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;
    public BattleUI battleUI;
    public MonsterSpawner monsterSpawner;
    [SerializeField] private BattleDeckViewer battleDeckViewer;
    public BattleDeckViewer DeckViewer => battleDeckViewer;
    [Tooltip("손패 선택 확인 버튼 (중앙에 놓인 카드 아래, 비우면 DeckPanel 아래 CheckBtn을 찾음)")]
    [SerializeField] private Button handSelectionConfirmButton;
    private HandCardSelectionController handCardSelection;

    [Header("Turn Settings")]
    // 드로우 수, 손패 상한, 턴당 에너지는 기본적으로 rules.json의 combat 항목에서 읽음
    // 실시간 테스트와 QA용으로 체크하면 아래 인스펙터 값을 대신 사용
    [SerializeField] private bool overrideTurnSettingsFromInspector = false;
    public int drawCardCount = 6;
    [SerializeField] private int maxHandCardCount = 10;
    [SerializeField] private int playerBaseEnergyPerTurn = 3;
    [SerializeField] private float enemyActionDelay = 0.2f;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button instantWinButton;
    [SerializeField] private bool enableKeyboardEndTurn = true;

    [Header("Spawned Monsters")]
    public List<Monster> spawnedMonsters = new List<Monster>();

    [Header("Debug")]
    public bool isDebugMode = false;
    [SerializeField] private DebugCardFilterType debugCardFilterType = DebugCardFilterType.Effect;
    public EffectType debugTargetEffect = EffectType.Barrier;
    [SerializeField] private DebugCardKeyword debugTargetKeyword = DebugCardKeyword.Keep;
    [SerializeField] private int debugTargetBuffId = 3002;
    [SerializeField] private int debugEnergyAmount = 1000;

    // Temporary target used while card effects are executing.
    public Monster currentTarget;
    private Monster previewDescriptionTarget;

    [Header("Run Data")]
    public static BuildingDeck buildingDeck;

    public BattleTurnState CurrentTurnState { get; private set; } = BattleTurnState.None;

    private TurnSystem turnSystem;
    private CombatResolver combatResolver;
    private EncounterSystem encounterSystem;
    private BattleBuffController battleBuffController;
    private CharacterIdentityService characterIdentityService;
    private bool hasResolvedBattleResult;
    [SerializeField] private TrainingMapController trainingMap;
    private bool hasStartedEncounter;
    private readonly List<PendingMonsterRevive> pendingMonsterRevives = new List<PendingMonsterRevive>();
    // 저장 덱 보상(획득·초과 제거) 진행 중에는 카드 사용과 턴 종료, 전투 종료 처리를 대기
    private bool isDeckRewardInProgress;
    // 카드 사용 연출 도중 턴 종료를 누름: 연출이 모두 끝날 때까지 카드 사용을 막고 기다렸다가 턴 종료
    private bool isEndTurnQueued;
    // R-47a 이번 전투에서 이탈한 몬스터가 훔쳐 간 카드 (승리 시 저장 덱에서 영구 제거)
    private readonly List<Card> pendingPermanentlyRemovedCards = new List<Card>();

    public float EnemyActionDelay => enemyActionDelay;
    public int MaxHandCardCount => Mathf.Max(0, overrideTurnSettingsFromInspector ? maxHandCardCount : GameRules.Combat.handLimit);
    public int DrawCardCount => Mathf.Max(0, overrideTurnSettingsFromInspector ? drawCardCount : GameRules.Combat.drawPerTurn);
    public int PlayerBaseEnergyPerTurn => Mathf.Max(0, overrideTurnSettingsFromInspector ? playerBaseEnergyPerTurn : GameRules.Combat.energyPerTurn);
    public bool IsHandFull => handManager != null && handManager.GetHandCount() >= MaxHandCardCount;
    public CharacterIdentityService CharacterIdentityService => characterIdentityService;

    // 버프 전용 호출은 이 진입점에서 담당 실행기에 바로 접근
    public BattleBuffController Buffs => battleBuffController ??= new BattleBuffController(this);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        encounterSystem = new EncounterSystem(this);
        turnSystem = new TurnSystem(this);
        combatResolver = new CombatResolver(this);
        battleBuffController = new BattleBuffController(this);
        characterIdentityService = new CharacterIdentityService(this);

        if (battleDeckViewer == null)
        {
            battleDeckViewer = FindAnyObjectByType<BattleDeckViewer>();
        }

        SetupHandCardSelection();
    }

    // 확인 버튼 자동 찾기: 지정한 부모 바로 아래의 CheckBtn
    private static Button FindConfirmButtonUnder(string parentName)
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name == "CheckBtn" && button.transform.parent != null && button.transform.parent.name == parentName)
            {
                return button;
            }
        }
        return null;
    }

    // 손패에서 고르는 선택은 패널 없이 전투 화면에서 진행 (확인 버튼은 씬 배치, 중앙 위치는 카드 사용 연출 지점)
    private void SetupHandCardSelection()
    {
        if (handSelectionConfirmButton == null)
        {
            handSelectionConfirmButton = FindConfirmButtonUnder("DeckPanel");
        }

        if (handSelectionConfirmButton == null)
        {
            Debug.LogWarning("[BattleManager] 손패 선택 확인 버튼이 없어 손패 선택도 목록으로 표시합니다");
            return;
        }

        handCardSelection = gameObject.AddComponent<HandCardSelectionController>();
        handCardSelection.Configure(handManager, handSelectionConfirmButton, handManager != null ? handManager.PlayCenterPosition : null);
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        UIModalStack.Changed += HandleModalChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
        UIModalStack.Changed -= HandleModalChanged;
    }

    // 패널이 열리면 진행 중인 드래그를 취소하고 카드 · 턴 종료 · 고유 스킬 버튼 상태를 다시 계산
    private void HandleModalChanged()
    {
        if (UIModalStack.BlocksBattleInput)
        {
            CardInteractionHandler.CancelActiveDrag();
        }

        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        battleUI?.UpdateAllUI();
    }

    private void Start()
    {
        if (BossModeSession.IsActive) isDebugMode = false;
        CardPlayEvents.OnCardPlayed += HandleCardClicked;

        if (!BossModeSession.IsActive && trainingMap != null)
        {
            if (!TrainingRunState.HasMapData)
                TrainingRunState.StartNewRun();
            PrepareRunOverview();
            if (!TrainingRunState.PendingNodeId.HasValue)
            {
                trainingMap.ShowMap();
                return;
            }
            if (trainingMap.TryOpenPendingRest()) return;
        }
        BeginSelectedBattle();
    }

    public void BeginSelectedBattle()
    {
        if (hasStartedEncounter) return;
        StopAllCoroutines();
        handCardSelection?.Cancel();
        battleDeckViewer?.ResetForCombat();
        handManager?.ClearHand();
        monsterSpawner?.ClearEncounter();
        spawnedMonsters.Clear();
        pendingMonsterRevives.Clear();
        currentTarget = null;
        previewDescriptionTarget = null;
        turnSystem = new TurnSystem(this);
        combatResolver = new CombatResolver(this);
        battleBuffController = new BattleBuffController(this);
        characterIdentityService = new CharacterIdentityService(this);
        hasStartedEncounter = true;
        trainingMap?.ShowBattle();
        InitializeCharacterSelection();
        InitializeBattle();
        InitializeInstantWinButton();

        battleUI?.UpdateAllUI();
        StartGame();
    }

    private void Update()
    {
        if (!hasStartedEncounter || !enableKeyboardEndTurn)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && CanEndPlayerTurn())
        {
            EndTurn();
        }
#endif
    }

    private void OnApplicationQuit()
    {
        BossModeSession.SaveResult();
    }

    private void OnDestroy()
    {
        BossModeSession.SaveResult();
        BossModeSession.Reset();
        CardPlayEvents.OnCardPlayed -= HandleCardClicked;
        if (instantWinButton != null)
        {
            instantWinButton.onClick.RemoveListener(OnClickInstantWin);
        }
    }

    private void HandleLanguageChanged()
    {
        RefreshLocalizedRuntimeCards();
        // 몬스터 의도 문구도 현재 언어로 다시 만듦
        if (encounterSystem != null)
        {
            foreach (Monster livingMonster in GetLivingMonsters())
            {
                livingMonster?.RefreshIntentText();
            }
        }

        UpdateAllUI();
    }

    private void InitializeInstantWinButton()
    {
        if (instantWinButton == null)
        {
            return;
        }

        instantWinButton.onClick.RemoveListener(OnClickInstantWin);
        instantWinButton.onClick.AddListener(OnClickInstantWin);
        instantWinButton.gameObject.SetActive(!BossModeSession.IsActive && (Application.isEditor || Debug.isDebugBuild));

        SetButtonLabel(instantWinButton, GameText.Ui("ui.debug.instantWin"));
        UpdateEndTurnButtonState();
    }

    private void OnClickInstantWin()
    {
        if (hasResolvedBattleResult || !(Application.isEditor || Debug.isDebugBuild))
        {
            return;
        }

        ResolveBattleResult(true);
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label ?? string.Empty;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label ?? string.Empty;
        }
    }

    public void RegisterMonster(Monster monster)
    {
        if (monster == null)
            return;

        if (!spawnedMonsters.Contains(monster))
        {
            spawnedMonsters.Add(monster);
            Debug.Log($"[BattleManager] Monster registered: {monster.name} (total: {spawnedMonsters.Count})");
        }
    }

    public void UnregisterMonster(Monster monster)
    {
        if (monster == null)
            return;

        if (spawnedMonsters.Contains(monster))
        {
            spawnedMonsters.Remove(monster);
            Debug.Log($"[BattleManager] Monster unregistered: {monster.name} (total: {spawnedMonsters.Count})");
        }
    }

    private void InitializeCharacterSelection()
    {
        if (SelectedButtonControl.selectedCharacterList == null ||
            SelectedButtonControl.selectedCharacterList.Count != 3)
        {
            Debug.LogWarning("[BattleManager] Character selection missing. Applying default test setup.");

            if (SelectedButtonControl.selectedCharacterList == null)
            {
                SelectedButtonControl.selectedCharacterList = new List<Character>();
            }

            SelectedButtonControl.selectedCharacterList.Clear();
            SelectedButtonControl.selectedCharacterList.Add(Character.Isla);
            SelectedButtonControl.selectedCharacterList.Add(Character.Ignia);
            SelectedButtonControl.selectedCharacterList.Add(Character.Polar);
        }
    }

    private static int CalculatePartyMaxHp()
    {
        int totalMaxHp = 0;
        foreach (Character character in SelectedButtonControl.selectedCharacterList)
        {
            CharacterData data = CharacterManager.GetCharacterByEnum(character);
            if (data != null)
            {
                totalMaxHp += data.maxHp;
            }
            else
            {
                Debug.LogWarning($"[BattleManager] CharacterData not found: {character}");
            }
        }

        return totalMaxHp;
    }

    // 지도 · 휴식 화면에서도 상단 바(체력 · 덱 버튼)를 쓸 수 있게 첫 전투 전에 런 덱 · 체력을 준비
    private void PrepareRunOverview()
    {
        InitializeCharacterSelection();
        if (buildingDeck == null && CardManager.IsInitialized())
        {
            buildingDeck = TrainingRunDeckPersistence.CreateRunDeck(SelectedButtonControl.selectedCharacterList);
        }

        if (!TrainingRunState.TryGetPlayerHealthState(out int hp, out int maxHp))
        {
            maxHp = CalculatePartyMaxHp();
            hp = maxHp;
            TrainingRunState.SetPlayerHealthState(hp, maxHp);
        }

        battleUI?.UpdateRunHealth(hp, maxHp);
    }

    private void InitializeBattle()
    {
        if (playerData == null)
        {
            playerData = PlayerData.Create();
        }

        int totalMaxHp = CalculatePartyMaxHp();
        int totalDefense = 0;

        playerData.Initialize(totalMaxHp, totalDefense, PlayerBaseEnergyPerTurn);
        characterIdentityService?.Initialize(SelectedButtonControl.selectedCharacterList);
        // 훈련 중에는 이전 전투의 아이덴티티 충전 진행도를 이어받고 보스전은 0에서 시작
        if (!BossModeSession.IsActive && TrainingRunState.IsRunActive)
        {
            characterIdentityService?.RestoreGauges(TrainingRunState.GetIdentityGauges());
        }

        if (TrainingRunState.IsRunActive)
        {
            if (TrainingRunState.TryGetPlayerHealthState(out int runHp, out int runMaxHp))
            {
                int resolvedMaxHp = runMaxHp > 0 ? runMaxHp : totalMaxHp;
                playerData.maxHP = resolvedMaxHp;
                playerData.hp = Mathf.Clamp(runHp, 0, resolvedMaxHp);
            }

            TrainingRunState.SetPlayerHealthState(playerData.hp, playerData.maxHP);
        }

        ApplyDebugEnergy();

        SpawnEncounterMonsters();

        battleContext = new BattleContext();

        if (!CardManager.IsInitialized())
        {
            Debug.LogError("[BattleManager] CardManager is not initialized.");
            return;
        }

        if (BossModeSession.IsActive) buildingDeck = BossModeSession.CreateDeck();
        bool shouldRebuildDeck = !BossModeSession.IsActive && (buildingDeck == null || !TrainingRunState.IsRunActive);
        if (shouldRebuildDeck)
        {
            Debug.Log("[BattleManager] 현재 캐릭터 선택 기준으로 런 덱을 다시 구성합니다");
            buildingDeck = TrainingRunDeckPersistence.CreateRunDeck(SelectedButtonControl.selectedCharacterList);
        }
        else
        {
            Debug.Log($"[BattleManager] Continuing existing run. Cards: {buildingDeck.CopyDeck().Count}");
        }

        if (usableDeckManager == null)
        {
            Debug.LogError("[BattleManager] UsableDeckManager is missing.");
            return;
        }

        List<Card> battleDeck = isDebugMode && !BossModeSession.IsActive
            ? BuildDebugBattleDeck()
            : buildingDeck.CopyDeck();
        usableDeckManager.SetDeck(battleDeck);

        UpdateAllUI();
    }

    private void StartGame()
    {
        if (!ValidateRuntimeReferences())
            return;

        SetState(BattleTurnState.CombatStart);
        hasResolvedBattleResult = false;

        battleContext.OnCombatStart();
        usableDeckManager.ShuffleDeck();

        turnSystem.ResetForCombat();
        battleBuffController?.ResetForCombat();
        ApplyCuriosityAtCombatStart();
        ApplyCombatStartEffects();

        if (TryHandleCombatEnd())
            return;

        turnSystem.BeginPlayerTurn();
    }

    public void DrawCards(int count, bool ignoreRootAbsorption = false)
    {
        DrawCardsAndGet(count, ignoreRootAbsorption);
    }

    public List<Card> DrawCardsAndGet(int count, bool ignoreRootAbsorption = false)
    {
        return DrawCardsSequentially(count, ignoreRootAbsorption, () => usableDeckManager?.DrawCard());
    }

    public System.Collections.IEnumerator DrawCardsCoroutine(int count, bool ignoreRootAbsorption = false, Action<List<Card>> onComplete = null)
    {
        yield return DrawCardsSequentiallyCoroutine(
            count,
            ignoreRootAbsorption,
            () => usableDeckManager?.DrawCard(),
            onComplete);
    }

    public void DrawBasicCards(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false)
    {
        DrawBasicCardsAndGet(count, characterFilter, ignoreRootAbsorption);
    }

    public List<Card> DrawBasicCardsAndGet(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false)
    {
        return DrawCardsSequentially(count, ignoreRootAbsorption, () =>
        {
            List<Card> drawnCards = usableDeckManager?.DrawBasicCards(1, characterFilter);
            return drawnCards != null && drawnCards.Count > 0 ? drawnCards[0] : null;
        });
    }

    public System.Collections.IEnumerator DrawBasicCardsCoroutine(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false, Action<List<Card>> onComplete = null)
    {
        yield return DrawCardsSequentiallyCoroutine(
            count,
            ignoreRootAbsorption,
            () =>
            {
                List<Card> drawnCards = usableDeckManager?.DrawBasicCards(1, characterFilter);
                return drawnCards != null && drawnCards.Count > 0 ? drawnCards[0] : null;
            },
            onComplete);
    }

    public void DrawCharacterCards(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false)
    {
        DrawCharacterCardsAndGet(count, characterFilter, ignoreRootAbsorption);
    }

    // 조건에 맞는 카드만 드로우 (다른 아군 카드, 피해를 입히는 카드 등)
    public List<Card> DrawMatchingCardsAndGet(int count, Predicate<Card> match, bool ignoreRootAbsorption = false)
    {
        return DrawCardsSequentially(count, ignoreRootAbsorption, () => usableDeckManager?.DrawMatchingCard(match));
    }

    public List<Card> DrawCharacterCardsAndGet(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false)
    {
        return DrawCardsSequentially(count, ignoreRootAbsorption, () =>
        {
            if (!characterFilter.HasValue)
            {
                return null;
            }

            List<Card> drawnCards = usableDeckManager?.DrawCharacterCards(1, characterFilter);
            return drawnCards != null && drawnCards.Count > 0 ? drawnCards[0] : null;
        });
    }

    public System.Collections.IEnumerator DrawCharacterCardsCoroutine(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false, Action<List<Card>> onComplete = null)
    {
        yield return DrawCardsSequentiallyCoroutine(
            count,
            ignoreRootAbsorption,
            () =>
            {
                if (!characterFilter.HasValue)
                {
                    return null;
                }

                List<Card> drawnCards = usableDeckManager?.DrawCharacterCards(1, characterFilter);
                return drawnCards != null && drawnCards.Count > 0 ? drawnCards[0] : null;
            },
            onComplete);
    }

    [ContextMenu("Debug Draw Matching Cards")]
    public void DebugDrawMatchingCards()
    {
        if (!isDebugMode || handManager == null) {
            return;
        }

        List<Card> matchingCards = CollectDebugMatchingCards();

        if (matchingCards.Count > 0) {
            Debug.Log($"[Debug] 디버그 시작 카드 {matchingCards.Count}장을 추가했습니다. 기준: {GetDebugTargetSummary()}");
            handManager.AddCardIgnoringHandLimit(matchingCards);
            if (battleContext != null) {
                battleContext.OnCardsDrawn(matchingCards.Count);
            }
        }
        else {
            Debug.LogWarning($"[Debug] 디버그 기준에 맞는 카드를 찾지 못했습니다. 기준: {GetDebugTargetSummary()}");
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    public void DebugDrawCardsByEffect()
    {
        DebugDrawMatchingCards();
    }

    private List<Card> CollectDebugMatchingCards()
    {
        List<Card> matchingCards = new List<Card>();
        List<CardData> allCardData = CardManager.GetAllCards();
        foreach (CardData cardData in allCardData)
        {
            if (cardData == null)
            {
                continue;
            }

            Card card = cardData.ToCard();
            if (card == null || !CardMatchesDebugTarget(card))
            {
                continue;
            }

            matchingCards.Add(card);
        }

        return matchingCards;
    }

    private bool CardMatchesDebugTarget(Card card)
    {
        if (card == null)
            return false;

        return debugCardFilterType switch
        {
            DebugCardFilterType.Keyword => card.HasKeyword((int)debugTargetKeyword),
            DebugCardFilterType.Buff => CardHasDebugTargetBuff(card),
            _ => CardHasDebugTargetEffect(card)
        };
    }

    private bool CardHasDebugTargetEffect(Card card)
    {
        return CardEffectListMatches(card?.effects, IsDebugTargetEffect)
            || CardEffectListMatches(card?.keepEffects, IsDebugTargetEffect)
            || CardEffectListMatches(card?.endTurnInHandEffects, IsDebugTargetEffect);
    }

    private bool CardHasDebugTargetBuff(Card card)
    {
        if (debugTargetBuffId <= 0)
        {
            return false;
        }

        return CardEffectListMatches(card?.effects, IsDebugTargetBuffEffect)
            || CardEffectListMatches(card?.keepEffects, IsDebugTargetBuffEffect)
            || CardEffectListMatches(card?.endTurnInHandEffects, IsDebugTargetBuffEffect);
    }

    private bool CardEffectListMatches(List<ICardEffect> effects, Func<ICardEffect, bool> matchPredicate)
    {
        if (effects == null || matchPredicate == null)
        {
            return false;
        }

        foreach (ICardEffect effect in effects)
        {
            if (EffectMatches(effect, matchPredicate))
            {
                return true;
            }
        }

        return false;
    }

    private bool EffectMatches(ICardEffect effect, Func<ICardEffect, bool> matchPredicate)
    {
        if (effect == null || matchPredicate == null)
        {
            return false;
        }

        if (matchPredicate(effect))
        {
            return true;
        }

        if (effect is AttackEffect attack && CardEffectListMatches(attack.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is DamageEffect damage && CardEffectListMatches(damage.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is DrawEffect draw && CardEffectListMatches(draw.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is DrawBasicEffect drawBasic && CardEffectListMatches(drawBasic.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is DrawCharacterEffect drawCharacter && CardEffectListMatches(drawCharacter.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is BarrierEffect barrier && CardEffectListMatches(barrier.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is ExhaustCardEffect exhaust && CardEffectListMatches(exhaust.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is CopyEffect copy && CardEffectListMatches(copy.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is MoveEffect move && CardEffectListMatches(move.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is SelectCardEffect selectCard && CardEffectListMatches(selectCard.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is ChangeStatEffect changeStat && CardEffectListMatches(changeStat.onActions, matchPredicate))
        {
            return true;
        }
        if (effect is ConditionalEffect conditional)
        {
            if (CardEffectListMatches(conditional.successEffects, matchPredicate))
            {
                return true;
            }

            if (CardEffectListMatches(conditional.failEffects, matchPredicate))
            {
                return true;
            }
        }
        if (effect is RepeatEffect repeat && EffectMatches(repeat.effectToRepeat, matchPredicate))
        {
            return true;
        }

        return false;
    }

    private bool IsDebugTargetEffect(ICardEffect effect)
    {
        if (effect == null)
        {
            return false;
        }

        switch (debugTargetEffect)
        {
            case EffectType.Barrier:
                return effect is BarrierEffect;
            case EffectType.Damage:
                return effect is DamageEffect;
            case EffectType.Draw:
                return effect is DrawEffect;
            case EffectType.DrawCharacter:
                return effect is DrawCharacterEffect;
            case EffectType.DrawBasic:
                return effect is DrawBasicEffect;
            case EffectType.RandGenerate:
                return effect is RandGenerateEffect;
            case EffectType.Move:
                return effect is MoveEffect;
            case EffectType.ExhaustCard:
                return effect is ExhaustCardEffect;
            case EffectType.Repeat:
                return effect is RepeatEffect;
            case EffectType.Trigger:
                return effect is TriggerEffect;
            case EffectType.Kill:
                return effect is KillEffect;
            case EffectType.ChangeStat:
                return effect is ChangeStatEffect;
            case EffectType.ReduceCost:
                return effect is ReduceCostEffect;
            case EffectType.Cost:
                return effect is CostEffect;
            case EffectType.ModifyCard:
                return effect is ModifyCardEffect;
            case EffectType.ModifyCards:
                return effect is ModifyCardsEffect;
            case EffectType.Upgrade:
                return effect is UpgradeEffect;
            case EffectType.ExtraTurn:
                return effect is ExtraTurnEffect;
            case EffectType.MixBuff:
                return effect is MixBuffEffect;
            case EffectType.EnemyHpLossHealPlayer:
                return effect is EnemyHpLossHealPlayerEffect;
            case EffectType.Party:
                return effect is PartyEffect;
            case EffectType.RemoveBuff:
                return effect is RemoveBuffEffect;
            case EffectType.MultiplyBarrier:
                return effect is MultiplyBarrierEffect;
            case EffectType.Stamina:
                return effect is StaminaEffect;
            case EffectType.Attack:
                return effect is AttackEffect;
            case EffectType.Heal:
                return effect is HealEffect;
            case EffectType.Buff:
                return effect is BuffEffect;
            case EffectType.Scry:
                return effect is ScryEffect;
            default:
                return false;
        }
    }

    private bool IsDebugTargetBuffEffect(ICardEffect effect)
    {
        return effect is BuffEffect buffEffect && buffEffect.buffId == debugTargetBuffId;
    }

    private string GetDebugTargetSummary()
    {
        string keywordName = KeywordDatabase.GetKeywordName((int)debugTargetKeyword);
        return debugCardFilterType switch
        {
            DebugCardFilterType.Keyword => $"Keyword / {(string.IsNullOrWhiteSpace(keywordName) ? debugTargetKeyword.ToString() : keywordName)}",
            DebugCardFilterType.Buff => $"Buff / {debugTargetBuffId}",
            _ => $"Effect / {debugTargetEffect}"
        };
    }

    private void HandleCardClicked(CardPlayEventData eventData)
    {
        ClearPreviewDescriptionTarget();
        combatResolver.TryPlayCard(eventData);
    }

    public void EndTurn()
    {
        if (CanEndPlayerTurn() && handManager != null && handManager.HasPendingUseAnimations)
        {
            StartCoroutine(EndTurnAfterUseAnimations());
            return;
        }

        if (!CanEndPlayerTurn() || !turnSystem.TryEndPlayerTurn())
        {
            Debug.LogWarning("[BattleManager] EndTurn ignored. It is not the player's actionable state.");
        }
    }

    // 사용 연출이 남아 있으면 누른 시점부터 카드 사용 · 턴 종료 입력을 막고, 연출이 모두 끝나면 턴 종료
    private System.Collections.IEnumerator EndTurnAfterUseAnimations()
    {
        isEndTurnQueued = true;
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        UpdateAllUI();
        try
        {
            // 기다리는 동안 설정 등 패널이 열리면 닫힐 때까지 턴 종료도 대기
            while ((handManager != null && handManager.HasPendingUseAnimations) || UIModalStack.BlocksBattleInput)
            {
                yield return null;
            }
        }
        finally
        {
            isEndTurnQueued = false;
        }

        if (!turnSystem.TryEndPlayerTurn())
        {
            Debug.LogWarning("[BattleManager] 사용 연출 후 턴 종료를 진행할 수 없는 상태입니다");
            UpdateEndTurnButtonState();
            RefreshHandPlayableState();
            UpdateAllUI();
        }
    }

    /// <summary>
    /// Allows card effects/boss rules to force end the player's turn.
    /// </summary>
    public void ForceEndPlayerTurn()
    {
        turnSystem.ForceEndPlayerTurn();
    }

    /// <summary>
    /// Extension point for effects that increase/decrease next turn draw.
    /// </summary>
    public void AddTurnStartDrawModifier(int amount)
    {
        turnSystem.AddTurnStartDrawModifier(amount);
    }

    public void AddExtraTurn(int amount)
    {
        turnSystem.AddExtraTurn(amount);
    }

    public void AddTurnEndTriggerRepeat(int amount)
    {
        turnSystem.AddTurnEndTriggerRepeat(amount);
    }

    public void ApplyCombatStartEffects()
    {
        if (usableDeckManager == null)
        {
            return;
        }

        List<Card> drawPile = usableDeckManager.GetDrawPile();
        if (drawPile.Count <= 0)
        {
            return;
        }

        List<Card> openingCards = new List<Card>();
        List<Card> remainingCards = new List<Card>();
        foreach (Card card in drawPile)
        {
            if (card != null && card.HasKeyword(CardKeywordIds.Opening))
            {
                openingCards.Add(card);
                continue;
            }

            remainingCards.Add(card);
        }

        if (openingCards.Count <= 0)
        {
            return;
        }

        ShuffleCards(openingCards);

        List<Card> reorderedDrawPile = new List<Card>(openingCards.Count + remainingCards.Count);
        reorderedDrawPile.AddRange(openingCards);
        reorderedDrawPile.AddRange(remainingCards);
        usableDeckManager.SetDrawPile(reorderedDrawPile);

        int currentHandCount = handManager != null ? handManager.GetHandCount() : 0;
        int availableHandSpace = Mathf.Max(0, MaxHandCardCount - currentHandCount);
        int targetStartDrawCount = Mathf.Min(availableHandSpace, Mathf.Max(DrawCardCount, openingCards.Count));
        int drawModifier = targetStartDrawCount - DrawCardCount;
        if (drawModifier != 0)
        {
            turnSystem.AddTurnStartDrawModifier(drawModifier);
        }

        UpdateAllUI();
    }

    private static void ShuffleCards(List<Card> cards)
    {
        if (cards == null || cards.Count <= 1)
        {
            return;
        }

        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }

    public void ApplyPlayerTurnStartEffects()
    {
        ApplyDebugEnergy();
        battleBuffController?.ApplyPlayerTurnStartEffects();
    }

    public void ApplyPlayerTurnEndEffects()
    {
        battleBuffController?.ApplyPlayerTurnEndEffects();
    }

    public void FinishEnemyTurnEndEffects()
    {
        battleBuffController?.Player.OnEnemyTurnEnd();
    }

    public void FinishPlayerTurnEndEffects()
    {
        battleBuffController?.FinishPlayerTurnEndEffects();
    }

    public void ResolveAdditionalTurnEndTriggers()
    {
        battleBuffController?.Player.ReplayTurnEndTriggeredEffects();
    }

    public bool TryHandleCombatEnd()
    {
        return encounterSystem.TryHandleCombatEnd();
    }

    public void ResolveBattleResult(bool isVictory)
    {
        if (hasResolvedBattleResult)
            return;

        hasResolvedBattleResult = true;
        BossModeSession.SaveResult();

        SetState(BattleTurnState.CombatEnd);
        battleBuffController?.Player.OnBattleEnded(isVictory);
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        UpdateAllUI();

        Debug.Log(isVictory
            ? "[BattleManager] Victory. All enemies are dead."
            : "[BattleManager] Defeat. Player is dead.");

        if (!isVictory)
        {
            if (battleUI != null)
            {
                battleUI.ShowDefeatPanel();
            }
            else
            {
                buildingDeck = null;
                PlayerData.Reset();
                TrainingRunState.ResetRun();
                SceneManager.LoadScene("LobbyScene");
            }

            return;
        }

        if (BossModeSession.IsActive)
        {
            StartCoroutine(ReturnToLobbyAfterBossVictory());
        }
        else if (TrainingRunState.IsRunActive)
        {
            StartCoroutine(HandleTrainingRunBattleResult(isVictory));
        }
    }

    public List<Monster> GetLivingMonsters()
    {
        return encounterSystem.GetLivingMonsters();
    }

    private System.Collections.IEnumerator ReturnToLobbyAfterBossVictory()
    {
        // 마지막 공격의 후속 효과 처리를 마친 뒤 전투 씬을 정리
        yield return null;
        buildingDeck = null;
        PlayerData.Reset();
        SceneManager.LoadScene("LobbyScene");
    }

    public bool HasPendingSelection => (handCardSelection != null && handCardSelection.IsActive)
        || (battleDeckViewer != null && battleDeckViewer.IsSelectionActive);

    public bool CanPlayerPlayCard()
    {
        // 패널(설정 · 덱 목록 · 선택 · 지도 미리보기 등)이 열려 있으면 전투 진행 정지
        return !UIModalStack.BlocksBattleInput
            && !(trainingMap != null && trainingMap.CoversBattle)
            && !HasPendingSelection && !isDeckRewardInProgress && !(combatResolver?.IsResolvingCardPlay ?? false)
            && !isEndTurnQueued
            && turnSystem.CanPlayerPlayCard();
    }

    public bool CanEndPlayerTurn()
    {
        return !UIModalStack.BlocksBattleInput
            && !(trainingMap != null && trainingMap.CoversBattle)
            && !HasPendingSelection && !isDeckRewardInProgress && !(combatResolver?.IsResolvingCardPlay ?? false)
            && !isEndTurnQueued
            && turnSystem.CanEndPlayerTurn();
    }

    public bool CanInteractWithCards()
    {
        return CanPlayerPlayCard();
    }

    public void UpdateEndTurnButtonState()
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = CanEndPlayerTurn();
        }

        if (instantWinButton != null)
        {
            instantWinButton.interactable = (Application.isEditor || Debug.isDebugBuild)
                && !hasResolvedBattleResult && CurrentTurnState != BattleTurnState.CombatEnd;
        }
    }

    public void RefreshHandPlayableState()
    {
        if (handManager == null)
            return;

        // 손패 선택 중에는 낼 수 있는지 대신 선택 후보 여부로 표시 (후보만 색 유지)
        if (handCardSelection != null && handCardSelection.IsActive)
        {
            handManager.RefreshCardPlayability(handCardSelection.IsCandidate, true);
            return;
        }

        bool canInteract = CanPlayerPlayCard();

        handManager.RefreshCardPlayability(
            card => playerData != null && card != null && CanPlayCard(card) && CanPayCardCost(card),
            canInteract);
    }

    public bool CanPlayCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        if (battleBuffController != null)
        {
            return battleBuffController.Player.CanPlayCard(card);
        }

        return card.CanBePlayed();
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        return battleBuffController != null && battleBuffController.Player.ShouldPotionGoToDiscardInsteadOfExhaust(card);
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        return battleBuffController != null && battleBuffController.Player.ShouldExhaustUnlockedUnplayableCard(card);
    }

    public bool CanDrawCards()
    {
        return battleBuffController == null || battleBuffController.Player.CanDrawCards();
    }

    public bool HasCardInHand(int cardId)
    {
        if (cardId <= 0 || handManager == null)
        {
            return false;
        }

        List<Card> handCards = handManager.GetHandCards();
        foreach (Card handCard in handCards)
        {
            if (handCard != null && handCard.cardId == cardId)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanGainCardsToHand()
    {
        return battleBuffController == null || battleBuffController.Player.CanGainCardsToHand();
    }

    public bool CanGainCardsToHandFrom(MoveZoneType from, string subject = null)
    {
        return battleBuffController == null || battleBuffController.Player.CanGainCardsToHandFrom(from, subject);
    }

    public int GetEffectiveCardCost(Card card)
    {
        if (card == null || card.isCurious)
        {
            return 0;
        }

        return battleBuffController != null ? battleBuffController.Player.GetEffectiveCardCost(card) : card.cost;
    }

    public bool CanPayCardCost(Card card)
    {
        if (card == null || playerData == null)
        {
            return false;
        }

        int effectiveCost = GetEffectiveCardCost(card);
        if (effectiveCost <= 0)
        {
            return true;
        }

        switch (card.costType)
        {
            case CardCostType.Barrier:
                return playerData.defense >= effectiveCost;
            case CardCostType.Rune:
                return playerData.GetBuffStack(BattleRuntimeDefinitions.RuneBuffId) >= effectiveCost;
            case CardCostType.Wave:
                return playerData.GetBuffStack(BattleRuntimeDefinitions.WaveBuffId) >= effectiveCost;
            default:
                return playerData.energy >= effectiveCost;
        }
    }

    public bool TryPayCardCost(Card card)
    {
        if (card == null || playerData == null)
        {
            return false;
        }

        int effectiveCost = GetEffectiveCardCost(card);
        if (effectiveCost <= 0)
        {
            playerData.ConsumeBuffStack(BattleRuntimeDefinitions.NextCardFreeBuffId, 1);
            return true;
        }

        if (card.costType == CardCostType.Barrier)
        {
            return playerData.RemoveDefense(effectiveCost) == effectiveCost;
        }

        if (card.costType == CardCostType.Rune)
        {
            if (playerData.GetBuffStack(BattleRuntimeDefinitions.RuneBuffId) < effectiveCost)
            {
                return false;
            }

            playerData.ConsumeBuffStack(BattleRuntimeDefinitions.RuneBuffId, effectiveCost);
            WuppiModeRuntimeUtility.HandleDirectBuffStackChange(this, playerData, BattleRuntimeDefinitions.RuneBuffId);
            return true;
        }

        if (card.costType == CardCostType.Wave)
        {
            if (playerData.GetBuffStack(BattleRuntimeDefinitions.WaveBuffId) < effectiveCost)
            {
                return false;
            }

            playerData.ConsumeBuffStack(BattleRuntimeDefinitions.WaveBuffId, effectiveCost);
            return true;
        }

        bool usedEnergy = playerData.UseEnergy(effectiveCost);
        if (usedEnergy)
        {
            battleContext?.OnEnergySpent(effectiveCost);
        }

        return usedEnergy;
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return battleBuffController != null ? battleBuffController.Player.GetCardUseAllEnemiesDamage() : 0;
    }

    public void ApplyCardUseAllEnemiesDamage(int damage)
    {
        battleBuffController?.ApplyCardUseAllEnemiesDamage(damage);
    }

    public int ResolvePlayerBarrierGain(int amount, bool fromCard = false)
    {
        return battleBuffController != null ? battleBuffController.ResolvePlayerBarrierGain(amount, fromCard) : Mathf.Max(0, amount);
    }

    public bool ShouldRetainPlayerBarrierOnTurnStart()
    {
        return battleBuffController != null && battleBuffController.ShouldRetainPlayerBarrierOnTurnStart();
    }

    public int GetPlayerTurnStartBarrierLoss(int currentDefense)
    {
        return battleBuffController != null ? battleBuffController.Player.GetTurnStartBarrierLoss(currentDefense) : Mathf.Max(0, currentDefense);
    }

    public int GetTurnEndRetainCount()
    {
        return battleBuffController != null ? battleBuffController.Player.GetTurnEndRetainCount() : 0;
    }

    public int GetCardBaseDamageBonus(Card sourceCard, bool isAttackEffect)
    {
        int permanentBonus = sourceCard != null ? Mathf.Max(0, sourceCard.permanentDamageBonus) : 0;
        return permanentBonus + (battleBuffController != null
            ? battleBuffController.Player.GetCardBaseDamageBonus(sourceCard, isAttackEffect)
            : 0);
    }

    // R-22a 런 덱(저장 덱 원본)에 영구 피해 증가를 반영 (보스전에서는 반영하지 않음)
    public void RecordPermanentDamageBonus(Card card, int amount)
    {
        if (card == null || amount == 0 || BossModeSession.IsActive || !TrainingRunState.IsRunActive || buildingDeck == null)
        {
            return;
        }

        if (!buildingDeck.AddPermanentDamageBonusAt(card.sourceDeckIndex, card.cardId, amount))
        {
            Debug.LogWarning($"[BattleManager] 저장 덱에서 카드 위치를 찾지 못해 이번 전투에만 피해를 늘렸습니다: {card.cardName}");
        }
    }

    public int ApplyCardDamageRuntimeModifiers(Card sourceCard, int damage)
    {
        return battleBuffController != null
            ? battleBuffController.Player.ModifyCardDamage(sourceCard, damage)
            : Mathf.Max(0, damage);
    }

    public int GetPlayerCalculatedCardDamageBonus(float strengthMultiplier)
    {
        return battleBuffController != null ? battleBuffController.Player.GetCalculatedCardDamageBonus(strengthMultiplier) : 0;
    }

    public float GetPlayerCalculatedCardBaseMultiplier(float baseMultiplier)
    {
        return battleBuffController != null ? battleBuffController.Player.GetCalculatedCardBaseMultiplier(baseMultiplier) : Mathf.Max(0f, baseMultiplier);
    }

    public float GetPlayerOutgoingDamageMultiplier()
    {
        return battleBuffController != null ? battleBuffController.Player.GetOutgoingDamageMultiplier() : 1f;
    }

    public int ResolvePlayerEffectDamage(int damage)
    {
        int safeDamage = Mathf.Max(0, damage);
        return Mathf.Max(0, Mathf.FloorToInt(safeDamage * GetPlayerOutgoingDamageMultiplier()));
    }

    public int ResolvePlayerIncomingDamage(int damage, Monster attacker)
    {
        return battleBuffController != null ? battleBuffController.ResolvePlayerIncomingDamage(damage, attacker) : Mathf.Max(0, damage);
    }

    public bool TryPreventPlayerIncomingDamage(int damage, Monster attacker)
    {
        return battleBuffController != null && battleBuffController.Player.TryPreventIncomingDamage(attacker, damage);
    }

    public int ResolvePersistentUpgradeCardId(int cardId)
    {
        return battleBuffController != null
            ? battleBuffController.ResolvePersistentUpgradeCardId(cardId)
            : cardId;
    }

    public int ConsumeRepeatedPlayCount(Card playedCard, bool isRepeatedEffect)
    {
        return battleBuffController != null ? battleBuffController.Player.ConsumeRepeatCount(playedCard, isRepeatedEffect) : 0;
    }

    public void HandlePlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        battleBuffController?.HandlePlayerAttackResolved(targetMonster, barrierBefore, barrierAfter);
    }

    public void HandleMonsterHpLost(Monster monster, int hpLoss)
    {
        if (monster != null && monster.IsBoss) BossModeSession.AddDamage(hpLoss);
        battleBuffController?.Monsters.OnMonsterHpLost(monster, hpLoss);
    }

    // 카드 효과로 파티가 체력을 잃었을 때 통계와 버프 알림을 함께 처리
    public void HandlePlayerCardHpLost(int hpLoss)
    {
        battleContext?.OnPlayerCardHpLost(hpLoss);
        battleBuffController?.Player.OnPlayerCardHpLost(hpLoss);
    }

    public void HandlePlayerHpLost(int hpLoss)
    {
        battleBuffController?.HandlePlayerHpLost(hpLoss);
    }

    public void HandleMonsterBuffApplied(Monster monster, int buffId, int amount)
    {
        battleBuffController?.Monsters.OnBuffApplied(monster, buffId, amount);
    }

    public void ApplyMonsterTurnStartEffects(Monster monster)
    {
        battleBuffController?.Monsters.OnMonsterTurnStart(monster);
    }

    public int GetMonsterBuffActivationCount(Monster monster, int buffId)
        => battleBuffController?.Monsters.GetActivationCount(monster, buffId) ?? 0;

    public void HandleMonsterAttackActionStarted(Monster monster)
    {
        battleBuffController?.Monsters.OnAttackActionStarted(monster);
        battleBuffController?.Player.OnEnemyAttackActionStarted(monster);
    }

    public void HandleMonsterAttackActionEnded(Monster monster)
        => battleBuffController?.Monsters.OnAttackActionEnded(monster);

    public void ApplyMonsterTurnEndEffects(Monster monster)
    {
        battleBuffController?.Monsters.OnMonsterTurnEnd(monster);
    }

    public bool ShouldKeepMonsterBarrierOnTurnStart(Monster monster)
    {
        return battleBuffController != null && battleBuffController.Monsters.ShouldKeepBarrierOnTurnStart(monster);
    }

    public int ResolveMonsterIncomingDamage(Monster monster, int damage)
    {
        return battleBuffController != null ? battleBuffController.ResolveMonsterIncomingDamage(monster, damage) : Mathf.Max(0, damage);
    }

    public void ConsumeMonsterIncomingDamageBuffs(Monster monster, int damage)
    {
        battleBuffController?.ConsumeMonsterIncomingDamageBuffs(monster, damage);
    }

    public void HandleMonsterDefenseChanged(Monster monster, int previousDefense, int currentDefense)
    {
        battleBuffController?.Monsters.OnDefenseChanged(monster, previousDefense, currentDefense);
        // 보호막 보유 여부가 바뀌면 조건 규칙형 의도 재판정
        if ((previousDefense > 0) != (currentDefense > 0))
        {
            ReevaluateConditionalMonsterIntents();
        }
    }

    // E-40 조건 규칙형 몬스터는 조건에 영향을 주는 이벤트(사망·이탈·보호막 변화) 직후 의도를 다시 정함
    public void ReevaluateConditionalMonsterIntents()
    {
        if (CurrentTurnState != BattleTurnState.PlayerAction)
        {
            return;
        }

        foreach (Monster livingMonster in GetLivingMonsters())
        {
            (livingMonster as DataMonster)?.ReevaluateConditionalIntent();
        }
    }

    public int ResolveMonsterBarrierGain(Monster monster, int amount)
    {
        return battleBuffController != null ? battleBuffController.Monsters.ResolveBarrierGain(monster, amount) : Mathf.Max(0, amount);
    }

    public int ModifyMonsterOutgoingDamage(Monster monster, int damage)
    {
        return battleBuffController != null ? battleBuffController.Monsters.ModifyOutgoingDamage(monster, damage) : Mathf.Max(0, damage);
    }

    public void HandleMonsterAttackResolved(Monster monster, PlayerData target, int attemptedDamage, int hpDamage)
    {
        battleBuffController?.Monsters.OnMonsterAttackResolved(monster, target, attemptedDamage, hpDamage);
    }

    public void HandleMonsterBeforeTakeDamage(Monster monster, int incomingDamage)
    {
        battleBuffController?.Monsters.OnMonsterBeforeTakeDamage(monster, incomingDamage);
    }

    public void HandleMonsterAfterTakeDamage(Monster monster, int incomingDamage, int damageAfterDefense)
    {
        battleBuffController?.Monsters.OnMonsterAfterTakeDamage(monster, incomingDamage, damageAfterDefense);
    }

    public bool CanMonsterReceiveDamage(Monster monster, int incomingDamage)
    {
        return battleBuffController == null || battleBuffController.Monsters.CanReceiveDamage(monster, incomingDamage);
    }

    public void HandleCardsExhausted(int count)
    {
        if (count <= 0)
        {
            return;
        }

        battleContext?.OnCardsExhausted(count);
        battleBuffController?.Player.OnCardsExhausted(count);
    }

    public void MoveCardToExhaust(Card card)
    {
        if (card == null)
        {
            return;
        }

        usableDeckManager?.AddToExhaust(card);
        HandleCardsExhausted(1);
    }

    public void MoveCardsToExhaust(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        usableDeckManager?.AddToExhaust(cards);
        HandleCardsExhausted(cards.Count);
    }

    public void RemoveCardFromCombat(Card card)
    {
        if (card == null)
        {
            return;
        }

        handManager?.RemoveCard(card);
        usableDeckManager?.RemoveFromCombat(card);
    }

    public int TriggerFeather(TargetType target, int repeatCount = 1)
    {
        return battleBuffController != null ? battleBuffController.TriggerFeather(target, repeatCount) : 0;
    }

    public int TriggerFeatherUntilEmpty(TargetType target)
    {
        return battleBuffController != null ? battleBuffController.TriggerFeatherUntilEmpty(target) : 0;
    }

    public System.Collections.IEnumerator ReplayExhaustedFeathersSequence()
    {
        if (battleBuffController != null) yield return battleBuffController.ReplayExhaustedFeathersSequence();
    }

    public bool TryConsumeSoulProtection()
    {
        return battleBuffController != null && battleBuffController.Player.TryConsumeFatalDamage();
    }

    public void HandleEnemyDebuffApplied(Monster monster, int buffId, int amount, int crueltyStackBeforeApply = -1)
    {
        if (!BuffData.IsHarmfulBuffId(buffId))
        {
            return;
        }

        battleContext?.OnEnemyDebuffApplied(buffId);
        battleBuffController?.HandleEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
    }

    // countsAsKill: 자폭처럼 처치가 아닌 사망은 사망 시 효과만 발동하고 처치 효과(처형자 등)는 발동하지 않음
    public void HandleMonsterDeath(Monster monster, bool countsAsKill = true)
    {
        battleBuffController?.Monsters.OnMonsterDeath(monster);
        if (countsAsKill)
        {
            battleBuffController?.Player.OnEnemyKilled(monster);
        }

        if (monster == null)
        {
            return;
        }

        if (currentTarget == monster)
        {
            currentTarget = null;
        }

        if (previewDescriptionTarget == monster)
        {
            previewDescriptionTarget = null;
        }

        UnregisterMonster(monster);

        if (monster.gameObject.activeSelf)
        {
            monster.gameObject.SetActive(false);
        }

        ReevaluateConditionalMonsterIntents();
        UpdateAllUI();
    }

    public void HandleMonsterLeaveCombat(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        battleBuffController?.Monsters.OnMonsterLeaveCombat(monster);

        if (currentTarget == monster)
        {
            currentTarget = null;
        }

        if (previewDescriptionTarget == monster)
        {
            previewDescriptionTarget = null;
        }

        UnregisterMonster(monster);

        if (monster.gameObject.activeSelf)
        {
            monster.gameObject.SetActive(false);
        }

        ReevaluateConditionalMonsterIntents();
        UpdateAllUI();
    }

    public void HandleMonsterRevived(Monster monster)
    {
        battleBuffController?.Monsters.OnMonsterRevived(monster);
    }

    public bool CanMonsterRevive(Monster monster)
    {
        return battleBuffController == null || battleBuffController.Monsters.CanRevive(monster);
    }

    public void ScheduleMonsterRevive(Monster monster, int turns)
    {
        if (monster == null || turns <= 0)
        {
            return;
        }

        for (int i = 0; i < pendingMonsterRevives.Count; i++)
        {
            PendingMonsterRevive pendingRevive = pendingMonsterRevives[i];
            if (pendingRevive == null || pendingRevive.monster != monster)
            {
                continue;
            }

            pendingRevive.remainingTurns = turns;
            return;
        }

        pendingMonsterRevives.Add(new PendingMonsterRevive
        {
            monster = monster,
            remainingTurns = turns
        });
    }

    public void ProcessPendingMonsterRevives()
    {
        if (pendingMonsterRevives.Count <= 0)
        {
            return;
        }

        for (int i = pendingMonsterRevives.Count - 1; i >= 0; i--)
        {
            PendingMonsterRevive pendingRevive = pendingMonsterRevives[i];
            if (pendingRevive == null || pendingRevive.monster == null)
            {
                pendingMonsterRevives.RemoveAt(i);
                continue;
            }

            pendingRevive.remainingTurns--;
            if (pendingRevive.remainingTurns > 0)
            {
                continue;
            }

            if (!CanMonsterRevive(pendingRevive.monster))
            {
                pendingMonsterRevives.RemoveAt(i);
                continue;
            }

            pendingMonsterRevives.RemoveAt(i);
            pendingRevive.monster.ReviveFromRespawn();
        }

        UpdateAllUI();
    }

    public Monster GetDescriptionTarget(bool previewOnly = false)
    {
        if (previewDescriptionTarget != null && !previewDescriptionTarget.IsDead())
        {
            return previewDescriptionTarget;
        }

        if (previewOnly)
        {
            return null;
        }

        if (currentTarget != null && !currentTarget.IsDead())
        {
            return currentTarget;
        }

        List<Monster> livingMonsters = GetLivingMonsters();
        if (livingMonsters != null && livingMonsters.Count == 1)
        {
            return livingMonsters[0];
        }

        return null;
    }

    public void SetPreviewDescriptionTarget(Monster target)
    {
        Monster nextTarget = target != null && !target.IsDead() ? target : null;
        Monster currentPreviewTarget = previewDescriptionTarget != null && !previewDescriptionTarget.IsDead()
            ? previewDescriptionTarget
            : null;

        if (currentPreviewTarget == nextTarget)
        {
            return;
        }

        previewDescriptionTarget = nextTarget;
        RefreshHandDescriptions();
    }

    public void ClearPreviewDescriptionTarget()
    {
        if (previewDescriptionTarget == null)
        {
            return;
        }

        previewDescriptionTarget = null;
        RefreshHandDescriptions();
    }

    public List<Card> ProcessGeneratedCards(List<Card> generatedCards, bool allowDuplicateGeneration = true)
    {
        if (generatedCards == null)
        {
            return new List<Card>();
        }

        return battleBuffController != null
            ? battleBuffController.ProcessGeneratedCards(generatedCards, allowDuplicateGeneration)
            : new List<Card>(generatedCards);
    }

    public Card ApplyHandCardUpgrades(Card card)
    {
        return ApplyPersistentUpgradeToCard(card);
    }

    public Card ApplyPersistentUpgradeToCard(Card card)
    {
        return ApplyPersistentUpgradeToCard(card, 0);
    }

    public Card ApplyPersistentUpgradeToCard(Card card, int sourceBuffId)
    {
        return battleBuffController != null
            ? battleBuffController.ApplyPersistentUpgradeToCard(card, Mathf.Max(0, sourceBuffId))
            : card;
    }

    public void ApplyBuffToPlayer(int buffId, int amount)
    {
        Buffs.ApplyToPlayer(buffId, amount);
    }

    public void ApplyBuffToMonster(Monster monster, int buffId, int amount)
    {
        Buffs.ApplyToMonster(monster, buffId, amount);
    }

    public void ApplyBuffToAllEnemies(int buffId, int amount)
    {
        Buffs.ApplyToAllEnemies(buffId, amount);
    }

    public void ChargeIdentityGauge(Character character, int amount = 1)
    {
        if (amount <= 0 || character == Character.Monster || characterIdentityService == null)
        {
            return;
        }

        characterIdentityService.AddGauge(character, amount);
    }

    // 직접 사용한 카드로 아이덴티티를 충전 (충전량은 rules.json의 chargeMode 기준)
    public void ChargeIdentityGaugeForPlayedCard(Card playedCard, int paidCost)
    {
        characterIdentityService?.ChargeForPlayedCard(playedCard, paidCost);
    }

    public void AddIdentityGaugeToOtherCharacters(Character sourceCharacter, int amount)
    {
        if (amount <= 0 || characterIdentityService == null)
        {
            return;
        }

        characterIdentityService.AddGaugeToOtherCharacters(sourceCharacter, amount);
    }

    public bool CanUseIdentitySkill(Character character)
    {
        return character != Character.Monster
            && CanPlayerPlayCard()
            && characterIdentityService != null
            && characterIdentityService.CanUse(character);
    }

    public bool TryUseIdentitySkill(Character character)
    {
        if (!CanUseIdentitySkill(character))
        {
            return false;
        }

        bool used = characterIdentityService.TryUse(character);
        if (!used)
        {
            return false;
        }

        // 훈련 중 전투마다 처음 조건을 채운 아이덴티티 사용은 해당 캐릭터의 신규 카드 획득 보상
        if (CanGrantIdentityCardReward() && characterIdentityService.TryClaimCardReward(character))
        {
            StartCoroutine(RunIdentityCardRewardSequence(character));
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        TryHandleCombatEnd();
        return true;
    }

    // R-20 전투 시작 시 강화되지 않은 카드 중 무작위 curiosityPerCombat장을 호기심 상태로 만듦
    // 보스전에는 호기심이 없음 (R-62)
    private void ApplyCuriosityAtCombatStart()
    {
        if (BossModeSession.IsActive || !TrainingRunState.IsRunActive || buildingDeck == null || usableDeckManager == null)
        {
            return;
        }

        List<Card> candidates = new List<Card>();
        foreach (Card card in usableDeckManager.GetDrawPile())
        {
            if (card != null && !card.isCurious && card.sourceDeckIndex >= 0
                && card.enforceCardIds != null && card.enforceCardIds.Count > 0)
            {
                candidates.Add(card);
            }
        }

        int curiosityCount = Mathf.Min(GameRules.Combat.curiosityPerCombat, candidates.Count);
        for (int i = 0; i < curiosityCount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            Card curiousCard = candidates[index];
            candidates.RemoveAt(index);
            curiousCard.isCurious = true;
            curiousCard.SetCost(0, false);
            Debug.Log($"[BattleManager] 호기심 카드: {curiousCard.cardName}");
        }
    }

    // R-22 호기심 카드 사용: 강화 계열 중 3개를 제시하고 고른 강화를 이번 전투와 저장 덱에 즉시 반영
    public System.Collections.IEnumerator PlayCuriositySequence(Card curiousCard)
    {
        if (curiousCard == null)
        {
            yield break;
        }

        List<Card> options = CardUpgradeOptions.PickRandom(curiousCard.enforceCardIds);

        curiousCard.isCurious = false;
        curiousCard.resolvedAsCuriosity = true;
        if (options.Count == 0)
        {
            Debug.LogWarning($"[BattleManager] {curiousCard.cardName}의 강화 선택지가 없어 호기심을 종료합니다");
            yield break;
        }

        List<Card> pickedCards = null;
        yield return SelectCardsCoroutine(options, 1, false, cards => pickedCards = cards);
        Card picked = pickedCards != null && pickedCards.Count > 0 ? pickedCards[0] : options[0];

        int originalCardId = curiousCard.cardId;
        int deckIndex = curiousCard.sourceDeckIndex;
        curiousCard.ApplyTemplate(picked);
        LocalizationManager.ApplyToRuntimeCard(curiousCard);
        if (buildingDeck != null && deckIndex >= 0 && deckIndex < buildingDeck.GetCards().Count
            && buildingDeck.GetCards()[deckIndex]?.cardId == originalCardId)
        {
            buildingDeck.ReplaceCardAt(deckIndex, picked.cardId);
        }
        else
        {
            Debug.LogWarning($"[BattleManager] 저장 덱에서 호기심 카드 위치를 찾지 못해 이번 전투에만 강화했습니다: {curiousCard.cardName}");
        }

        UpdateAllUI();
    }

    private bool CanGrantIdentityCardReward()
    {
        return !BossModeSession.IsActive && TrainingRunState.IsRunActive && buildingDeck != null;
    }

    private System.Collections.IEnumerator RunIdentityCardRewardSequence(Character character)
    {
        isDeckRewardInProgress = true;
        try
        {
            // 스킬 효과가 연 선택 창이 끝난 뒤 보상 선택을 진행
            while (HasPendingSelection)
            {
                yield return null;
            }

            List<Card> options = new List<Card>();
            foreach (int cardId in TrainingDeckRewardService.GetAcquisitionOptions(buildingDeck.GetCards(), character))
            {
                Card option = CardManager.GetCardAsCard(cardId);
                if (option != null)
                {
                    options.Add(option);
                }
            }

            if (options.Count == 0)
            {
                Debug.LogWarning($"[BattleManager] {character}의 획득 가능한 고유 카드가 없어 보상을 건너뜁니다");
                yield break;
            }

            List<Card> pickedCards = null;
            yield return SelectCardsCoroutine(options, 1, false, cards => pickedCards = cards);
            Card pickedCard = pickedCards != null && pickedCards.Count > 0 ? pickedCards[0] : options[0];
            if (buildingDeck.AddCard(pickedCard.cardId))
            {
                Debug.Log($"[BattleManager] 아이덴티티 보상으로 {pickedCard.cardName} 카드를 저장 덱에 추가했습니다");
                AddRewardCardToCurrentBattle(pickedCard.cardId);
            }

            yield return TrimCharacterDeckOverflowCoroutine(character);
        }
        finally
        {
            isDeckRewardInProgress = false;
            // 보상 진행 중 막아 둔 카드 사용과 턴 종료 상태를 다시 계산
            UpdateEndTurnButtonState();
            RefreshHandPlayableState();
            UpdateAllUI();
        }
    }

    // 전투 중 획득한 보상 카드는 이번 전투에서도 쓸 수 있도록 버린 카드 더미에 넣음
    private void AddRewardCardToCurrentBattle(int cardId)
    {
        if (usableDeckManager == null)
        {
            return;
        }

        Card battleCard = CardManager.GetCardAsCard(cardId);
        if (battleCard == null)
        {
            return;
        }

        LocalizationManager.ApplyToRuntimeCard(battleCard);
        usableDeckManager.AddToDiscard(battleCard);
        Debug.Log($"[BattleManager] 보상 카드 {battleCard.cardName}을(를) 버린 카드 더미에 넣었습니다");
    }

    // R-47a 이탈한 몬스터가 훔쳐 간 전투 카드를 저장 덱에서 제거할 대상으로 등록
    public void QueueStolenCardsForPermanentRemoval(IEnumerable<Card> stolenCards)
    {
        if (stolenCards == null || BossModeSession.IsActive || !TrainingRunState.IsRunActive)
        {
            return;
        }

        foreach (Card stolenCard in stolenCards)
        {
            if (stolenCard != null && stolenCard.sourceDeckIndex >= 0 && !pendingPermanentlyRemovedCards.Contains(stolenCard))
            {
                pendingPermanentlyRemovedCards.Add(stolenCard);
            }
        }
    }

    // 저장 덱 위치가 뒤인 카드부터 제거해 앞 카드의 위치가 바뀌지 않게 함
    private void ApplyPendingPermanentCardRemovals()
    {
        if (buildingDeck == null || pendingPermanentlyRemovedCards.Count == 0)
        {
            return;
        }

        List<Card> removals = new List<Card>(pendingPermanentlyRemovedCards);
        removals.Sort((left, right) => right.sourceDeckIndex.CompareTo(left.sourceDeckIndex));
        foreach (Card removedCard in removals)
        {
            if (!buildingDeck.RemoveCardAt(removedCard.sourceDeckIndex, removedCard.cardId))
            {
                Debug.LogWarning($"[BattleManager] 훔쳐 간 카드를 저장 덱에서 찾지 못했습니다: {removedCard.cardName}");
            }
        }

        pendingPermanentlyRemovedCards.Clear();
    }

    // R-46 네임드 승리 보상: 카드 복제(강화 상태 유지, 유일 제외, 8장 초과 시 R-15) 또는 카드 제거
    private System.Collections.IEnumerator RunNamedRewardSequence()
    {
        isDeckRewardInProgress = true;
        try
        {
            // 복제 · 제거 중 하나를 랜덤으로 제공 (고를 카드가 없으면 다른 쪽으로)
            bool isDuplicate = UnityEngine.Random.Range(0, 2) == 0;
            List<Card> candidates = GetNamedRewardCandidates(isDuplicate);
            if (candidates.Count == 0)
            {
                isDuplicate = !isDuplicate;
                candidates = GetNamedRewardCandidates(isDuplicate);
            }

            if (candidates.Count == 0)
            {
                yield break;
            }

            Debug.Log($"[BattleManager] 네임드 보상: {(isDuplicate ? "카드 복제" : "카드 제거")}");

            List<Card> pickedCards = null;
            yield return SelectCardsCoroutine(candidates, 1, false, cards => pickedCards = cards);
            Card picked = pickedCards != null && pickedCards.Count > 0 ? pickedCards[0] : null;
            if (picked == null)
            {
                yield break;
            }

            if (isDuplicate)
            {
                if (buildingDeck.AddCardCopy(picked))
                {
                    yield return TrimCharacterDeckOverflowCoroutine(picked.character);
                }
            }
            else
            {
                buildingDeck.RemoveCard(picked);
            }
        }
        finally
        {
            isDeckRewardInProgress = false;
        }
    }

    private List<Card> GetNamedRewardCandidates(bool isDuplicate)
    {
        return isDuplicate
            ? TrainingDeckRewardService.GetDuplicateCandidates(buildingDeck.GetCards())
            : TrainingDeckRewardService.GetRemovalCandidates(buildingDeck.GetCards());
    }

    // R-15 캐릭터 카드가 상한을 넘으면 넘은 장수만큼 골라 저장 덱에서 제거
    public System.Collections.IEnumerator TrimCharacterDeckOverflowCoroutine(Character character)
    {
        if (buildingDeck == null)
        {
            yield break;
        }

        int overflowCount = TrainingDeckRewardService.GetOverflowCount(buildingDeck.GetCards(), character);
        if (overflowCount <= 0)
        {
            yield break;
        }

        List<Card> characterCards = TrainingDeckRewardService.GetCharacterCards(buildingDeck.GetCards(), character);
        List<Card> removedCards = null;
        yield return SelectCardsCoroutine(characterCards, overflowCount, false, cards => removedCards = cards);
        if (removedCards == null || removedCards.Count < overflowCount)
        {
            // 선택 창이 없으면 가장 최근에 얻은 카드부터 제거
            removedCards = characterCards.GetRange(characterCards.Count - overflowCount, overflowCount);
        }

        foreach (Card removedCard in removedCards)
        {
            buildingDeck.RemoveCard(removedCard);
        }
    }

    private System.Collections.IEnumerator SelectCardsCoroutine(List<Card> cards, int selectCount, bool allowFewer, Action<List<Card>> onSelected)
    {
        bool isCompleted = false;
        List<Card> selectedCards = null;
        bool opened = OpenSelectCardPanel(cards, selectCount, result =>
        {
            selectedCards = result;
            isCompleted = true;
        }, allowFewer);

        if (opened)
        {
            yield return new WaitUntil(() => isCompleted);
        }

        onSelected?.Invoke(selectedCards);
    }

    public bool TryUseIdentitySkill(int characterId)
    {
        return TryUseIdentitySkill(CharacterManager.GetCharacterEnumById(characterId));
    }

    public bool CanUseIdentitySkillBySlot(int slotIndex)
    {
        if (!TryGetSelectedCharacterBySlot(slotIndex, out Character character))
        {
            return false;
        }

        return CanUseIdentitySkill(character);
    }

    public bool TryUseIdentitySkillBySlot(int slotIndex)
    {
        if (!TryGetSelectedCharacterBySlot(slotIndex, out Character character))
        {
            return false;
        }

        return TryUseIdentitySkill(character);
    }

    public void OnClickUseFirstIdentitySkill()
    {
        TryUseIdentitySkillBySlot(0);
    }

    public void OnClickUseSecondIdentitySkill()
    {
        TryUseIdentitySkillBySlot(1);
    }

    public void OnClickUseThirdIdentitySkill()
    {
        TryUseIdentitySkillBySlot(2);
    }

    public int GetIdentityGauge(Character character)
    {
        return characterIdentityService != null ? characterIdentityService.GetGauge(character) : 0;
    }

    public int GetIdentityCost(Character character)
    {
        return characterIdentityService != null ? characterIdentityService.GetCost(character) : 0;
    }

    public string GetIdentityDescription(Character character)
    {
        return characterIdentityService != null ? characterIdentityService.GetDescription(character) : string.Empty;
    }

    public IReadOnlyList<CharacterIdentityState> GetIdentityStates()
    {
        return characterIdentityService != null
            ? characterIdentityService.GetStates()
            : Array.Empty<CharacterIdentityState>();
    }

    public bool TryGetSelectedCharacterBySlot(int slotIndex, out Character character)
    {
        character = Character.Monster;

        IReadOnlyList<CharacterIdentityState> identityStates = GetIdentityStates();
        if (slotIndex < 0 || slotIndex >= identityStates.Count)
        {
            return false;
        }

        character = identityStates[slotIndex].Character;
        return character != Character.Monster;
    }


    private bool ValidateRuntimeReferences()
    {
        if (!CardManager.IsInitialized())
        {
            Debug.LogError("[BattleManager] CardManager is not initialized.");
            return false;
        }

        if (usableDeckManager == null)
        {
            Debug.LogError("[BattleManager] UsableDeckManager is missing.");
            return false;
        }

        if (handManager == null)
        {
            Debug.LogError("[BattleManager] HandManager is missing.");
            return false;
        }

        if (playerData == null)
        {
            Debug.LogError("[BattleManager] PlayerData is missing.");
            return false;
        }

        return true;
    }

    public int TurnSequence { get; private set; }

    public void SetState(BattleTurnState newState)
    {
        if (newState == BattleTurnState.PlayerTurnStart || newState == BattleTurnState.EnemyTurnStart)
            TurnSequence++;
        CurrentTurnState = newState;
    }

    public void UpdateAllUI()
    {
        RefreshHandDescriptions();

        if (battleUI != null)
            battleUI.UpdateAllUI();
    }

    private void RefreshHandDescriptions()
    {
        if (handManager == null)
        {
            return;
        }

        handManager.RefreshCardDisplays(handManager.GetHandCards());
    }

    private void RefreshLocalizedRuntimeCards()
    {
        if (handManager != null)
        {
            LocalizationManager.ApplyToRuntimeCards(handManager.GetHandCards());
        }

        if (usableDeckManager != null)
        {
            LocalizationManager.ApplyToRuntimeCards(usableDeckManager.GetDrawPile());
            LocalizationManager.ApplyToRuntimeCards(usableDeckManager.GetDiscardPile());
            LocalizationManager.ApplyToRuntimeCards(usableDeckManager.GetExhaustPile());
        }
    }

    public bool OpenSelectCardPanel(List<Card> selectableCards, int selectCount, Action<List<Card>> onSelected, bool allowFewer = false)
    {
        // 후보가 모두 손패 카드면 손패에서 고르기, 더미 · 덱 · 생성 후보가 섞이면 목록에서 고르기
        if (handCardSelection != null && handCardSelection.CanSelectFromHand(selectableCards))
        {
            return handCardSelection.Open(selectableCards, selectCount, allowFewer, onSelected);
        }

        if (battleDeckViewer == null)
        {
            Debug.LogWarning("[BattleManager] BattleDeckViewer is missing. Fallback to auto selection.");
            return false;
        }

        return battleDeckViewer.OpenSelectionPanel(selectableCards, selectCount, onSelected, allowFewer);
    }

    public void RegisterMonsterHpLossHealPlayerThisTurn(Monster monster)
    {
        battleBuffController?.RegisterMonsterHpLossHealPlayerThisTurn(monster);
    }

    private void ApplyDebugEnergy()
    {
        if (!isDebugMode || playerData == null)
            return;

        playerData.baseMaxEnergy = debugEnergyAmount;
        playerData.maxEnergy = debugEnergyAmount;
        CreamBuffRuntimeUtility.SyncEnergyOverflow(playerData);
        playerData.energy = playerData.maxEnergy;
    }

    private void SpawnEncounterMonsters()
    {
        if (monsterSpawner == null)
        {
            Debug.LogWarning("[BattleManager] MonsterSpawner is not assigned.");
            return;
        }

        if (BossModeSession.IsActive)
        {
            // 보스 조우(BS01 ~ BS04)의 동행 몬스터까지 함께 생성
            foreach (Monster bossEncounterMonster in monsterSpawner.SpawnEncounter(BossModeSession.GetBossEncounterId(BossModeSession.SelectedBossId)))
            {
                RegisterMonster(bossEncounterMonster);
            }
            return;
        }

        TrainingMapNodeData encounterNode = ResolveCurrentEncounterNode();
        List<Monster> encounterMonsters = encounterNode != null && encounterNode.HasPlannedEncounter
            ? monsterSpawner.SpawnEncounter(encounterNode.plannedEncounter)
            : monsterSpawner.SpawnEncounter(encounterNode != null ? encounterNode.nodeType : TrainingNodeType.Monster);

        foreach (Monster monster in encounterMonsters)
        {
            RegisterMonster(monster);
        }
    }

    private TrainingMapNodeData ResolveCurrentEncounterNode()
    {
        if (TrainingRunState.PendingNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode))
        {
            return pendingNode;
        }

        return null;
    }

    private List<Card> BuildDebugBattleDeck()
    {
        List<Card> debugDeck = new List<Card>();
        HashSet<int> addedCardIds = new HashSet<int>();

        if (SelectedButtonControl.selectedCharacterList == null)
        {
            return debugDeck;
        }

        foreach (Character character in SelectedButtonControl.selectedCharacterList)
        {
            List<CardData> cardsByCharacter = CardManager.GetCardsByCharacter(character);
            if (cardsByCharacter == null)
            {
                continue;
            }

            foreach (CardData cardData in cardsByCharacter)
            {
                if (cardData == null || !addedCardIds.Add(cardData.cardId))
                {
                    continue;
                }

                Card card = cardData.ToCard();
                if (card != null)
                {
                    debugDeck.Add(card);
                }
            }
        }

        Debug.Log($"[BattleManager] 디버그 덱 구성 완료: {debugDeck.Count}장");
        return debugDeck;
    }

    private List<Card> DrawCardsSequentially(int count, bool ignoreRootAbsorption, Func<Card> drawCard)
    {
        List<Card> drawnCards = new List<Card>();
        if (count <= 0 || drawCard == null || handManager == null || usableDeckManager == null)
        {
            return drawnCards;
        }

        if (!CanStartDraw(ignoreRootAbsorption))
        {
            return drawnCards;
        }

        for (int drawIndex = 0; drawIndex < count; drawIndex++)
        {
            if (!ignoreRootAbsorption && HasRootAbsorptionInHand())
            {
                break;
            }

            // 손패가 가득 차면 더 뽑지 않고 남은 카드는 덱에 그대로 둠
            if (IsHandFull)
            {
                break;
            }

            Card drawnCard = drawCard();
            if (drawnCard == null)
            {
                break;
            }

            drawnCards.Add(drawnCard);
            handManager.AddCard(drawnCard, true);
            drawnCard.ExecuteOnDrawEffects(this);

            if (!ignoreRootAbsorption && drawnCard.cardId == 40)
            {
                break;
            }
        }

        CompleteDrawSequence(drawnCards);
        return drawnCards;
    }

    private System.Collections.IEnumerator DrawCardsSequentiallyCoroutine(
        int count,
        bool ignoreRootAbsorption,
        Func<Card> drawCard,
        Action<List<Card>> onComplete)
    {
        List<Card> drawnCards = new List<Card>();
        if (count <= 0 || drawCard == null || handManager == null || usableDeckManager == null)
        {
            onComplete?.Invoke(drawnCards);
            yield break;
        }

        if (!CanStartDraw(ignoreRootAbsorption))
        {
            onComplete?.Invoke(drawnCards);
            yield break;
        }

        for (int drawIndex = 0; drawIndex < count; drawIndex++)
        {
            if (!ignoreRootAbsorption && HasRootAbsorptionInHand())
            {
                break;
            }

            // 손패가 가득 차면 더 뽑지 않고 남은 카드는 덱에 그대로 둠
            if (IsHandFull)
            {
                break;
            }

            Card drawnCard = drawCard();
            if (drawnCard == null)
            {
                break;
            }

            drawnCards.Add(drawnCard);
            Coroutine drawAnimation = handManager.AddCard(drawnCard, true);
            drawnCard.ExecuteOnDrawEffects(this);

            if (drawAnimation != null)
            {
                yield return drawAnimation;
            }
            else
            {
                yield return null;
            }

            if (!ignoreRootAbsorption && drawnCard.cardId == 40)
            {
                break;
            }
        }

        CompleteDrawSequence(drawnCards);
        onComplete?.Invoke(drawnCards);
    }

    private void CompleteDrawSequence(List<Card> drawnCards)
    {
        if (battleContext != null && drawnCards.Count > 0)
        {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    private bool CanStartDraw(bool ignoreRootAbsorption)
    {
        if (!CanDrawCards())
        {
            return false;
        }

        return ignoreRootAbsorption || !HasRootAbsorptionInHand();
    }

    private bool HasRootAbsorptionInHand()
    {
        return HasCardInHand(40);
    }

    private System.Collections.IEnumerator HandleTrainingRunBattleResult(bool isVictory)
    {
        // 승리를 판정한 카드 효과와 턴 처리가 현재 프레임에서 마무리되도록 대기
        yield return null;

        // 마지막 행동이 아이덴티티였다면 카드 획득 보상을 마친 뒤 저장 덱을 처리
        while (isDeckRewardInProgress)
        {
            yield return null;
        }

        if (!TrainingRunState.HasMapData)
            yield break;

        bool isBossVictory = false;
        if (isVictory
            && TrainingRunState.PendingNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode))
        {
            isBossVictory = pendingNode.nodeType == TrainingNodeType.Boss;
        }

        bool shouldPersistRunDeck = isBossVictory;

        if (isVictory)
        {
            ApplyPendingPermanentCardRemovals();

            if (TrainingRunState.PendingNodeId.HasValue
                && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData rewardNode)
                && rewardNode.nodeType == TrainingNodeType.Named
                && buildingDeck != null)
            {
                yield return RunNamedRewardSequence();
            }
        }

        pendingPermanentlyRemovedCards.Clear();

        if (PlayerData.Instance != null)
        {
            TrainingRunState.SetPlayerHealthState(PlayerData.Instance.hp, PlayerData.Instance.maxHP);
        }

        if (characterIdentityService != null)
        {
            TrainingRunState.SetIdentityGauges(characterIdentityService.ExportGauges());
        }

        if (shouldPersistRunDeck)
        {
            TrainingRunDeckPersistence.SaveRunDeckAsPermanentDeck(
                buildingDeck,
                SelectedButtonControl.selectedCharacterList,
                "보스 클리어로 저장덱을 갱신했습니다");
        }

        TrainingRunState.CompletePendingNode(isVictory);

        if (!isVictory || TrainingRunState.IsRunCompleted || TrainingRunState.IsRunFailed)
        {
            buildingDeck = null;
        }

        hasStartedEncounter = false;
        trainingMap.ShowMap();
    }
}

