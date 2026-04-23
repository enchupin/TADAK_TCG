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

    [Header("Turn Settings")]
    public int drawCardCount = 6;
    [SerializeField] private int maxHandCardCount = 10;
    [SerializeField] private int playerBaseEnergyPerTurn = 3;
    [SerializeField] private float enemyActionDelay = 0.2f;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private bool showInstantWinButton = true;
    [SerializeField] private bool enableKeyboardEndTurn = true;

    [Header("Run Flow")]
    [SerializeField] private float battleResultTransitionDelay = 0.8f;

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
    private Action<List<Monster>> pendingMonsterSelectionCallback;
    private readonly List<Monster> selectableMonsters = new List<Monster>();
    private readonly List<Monster> selectedMonsters = new List<Monster>();
    private int requiredMonsterSelectionCount;
    private bool isMonsterSelectionActive;

    [Header("Run Data")]
    public static BuildingDeck buildingDeck;

    public BattleTurnState CurrentTurnState { get; private set; } = BattleTurnState.None;

    private TurnSystem turnSystem;
    private CombatResolver combatResolver;
    private EncounterSystem encounterSystem;
    private BattleBuffController battleBuffController;
    private bool hasResolvedBattleResult;
    private Button instantWinButton;
    private readonly List<PendingMonsterRevive> pendingMonsterRevives = new List<PendingMonsterRevive>();

    public float EnemyActionDelay => enemyActionDelay;
    public int MaxHandCardCount => Mathf.Max(0, maxHandCardCount);

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

        if (battleDeckViewer == null)
        {
            battleDeckViewer = FindAnyObjectByType<BattleDeckViewer>();
        }
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
    }

    private void Start()
    {
        CardPlayEvents.OnCardPlayed += HandleCardClicked;

        InitializeCharacterSelection();
        InitializeBattle();
        CreateInstantWinButton();

        battleUI?.UpdateAllUI();
        StartGame();
    }

    private void Update()
    {
        if (!enableKeyboardEndTurn)
            return;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            EndTurn();
        }
#endif
    }

    private void OnDestroy()
    {
        CardPlayEvents.OnCardPlayed -= HandleCardClicked;
        if (instantWinButton != null)
        {
            instantWinButton.onClick.RemoveListener(OnClickInstantWin);
        }
    }

    private void HandleLanguageChanged()
    {
        RefreshLocalizedRuntimeCards();
        UpdateAllUI();
    }

    private void CreateInstantWinButton()
    {
        if (!showInstantWinButton || instantWinButton != null || endTurnButton == null)
        {
            return;
        }

        RectTransform parent = endTurnButton.transform.parent as RectTransform;
        RectTransform endTurnRect = endTurnButton.transform as RectTransform;
        if (parent == null || endTurnRect == null)
        {
            return;
        }

        instantWinButton = Instantiate(endTurnButton, parent);
        instantWinButton.name = "InstantWinButton";
        instantWinButton.onClick.RemoveAllListeners();
        instantWinButton.onClick.AddListener(OnClickInstantWin);

        RectTransform instantWinRect = instantWinButton.transform as RectTransform;
        if (instantWinRect != null)
        {
            instantWinRect.anchorMin = endTurnRect.anchorMin;
            instantWinRect.anchorMax = endTurnRect.anchorMax;
            instantWinRect.pivot = endTurnRect.pivot;
            instantWinRect.sizeDelta = endTurnRect.sizeDelta;
            instantWinRect.anchoredPosition = endTurnRect.anchoredPosition + new Vector2(-180f, 0f);
            instantWinRect.localScale = Vector3.one;
        }

        SetButtonLabel(instantWinButton, "승리");
        UpdateEndTurnButtonState();
    }

    private void OnClickInstantWin()
    {
        if (hasResolvedBattleResult)
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

    private void InitializeBattle()
    {
        if (playerData == null)
        {
            playerData = PlayerData.Create();
        }

        int totalMaxHp = 0;
        int totalDefense = 0;

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

        playerData.Initialize(totalMaxHp, totalDefense, playerBaseEnergyPerTurn);

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

        bool shouldRebuildDeck = buildingDeck == null || !TrainingRunState.IsRunActive;
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

        List<Card> battleDeck = isDebugMode
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

    public void DrawCharacterCards(int count, Character? characterFilter = null, bool ignoreRootAbsorption = false)
    {
        DrawCharacterCardsAndGet(count, characterFilter, ignoreRootAbsorption);
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

    [ContextMenu("Debug Draw Matching Cards")]
    public void DebugDrawMatchingCards()
    {
        if (!isDebugMode || handManager == null) {
            return;
        }

        List<Card> matchingCards = CollectDebugMatchingCards();

        if (matchingCards.Count > 0) {
            Debug.Log($"[Debug] 디버그 시작 카드 {matchingCards.Count}장을 추가했습니다. 기준: {GetDebugTargetSummary()}");
            handManager.AddCard(matchingCards);
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
        if (!turnSystem.TryEndPlayerTurn())
        {
            Debug.LogWarning("[BattleManager] EndTurn ignored. It is not the player's actionable state.");
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
        int targetStartDrawCount = Mathf.Min(availableHandSpace, Mathf.Max(drawCardCount, openingCards.Count));
        int drawModifier = targetStartDrawCount - drawCardCount;
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

    public void ResolveAdditionalTurnEndTriggers()
    {
        battleBuffController?.ReplayAdditionalTurnEndTriggers();
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

        SetState(BattleTurnState.CombatEnd);
        battleBuffController?.HandleBattleEnded(isVictory);
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        UpdateAllUI();

        Debug.Log(isVictory
            ? "[BattleManager] Victory. All enemies are dead."
            : "[BattleManager] Defeat. Player is dead.");

        if (TrainingRunState.IsRunActive)
        {
            StartCoroutine(HandleTrainingRunBattleResult(isVictory));
        }
    }

    public List<Monster> GetLivingMonsters()
    {
        return encounterSystem.GetLivingMonsters();
    }

    public bool CanPlayerPlayCard()
    {
        return !isMonsterSelectionActive && turnSystem.CanPlayerPlayCard();
    }

    public bool CanEndPlayerTurn()
    {
        return !isMonsterSelectionActive && turnSystem.CanEndPlayerTurn();
    }

    public bool CanInteractWithCards()
    {
        return !isMonsterSelectionActive && turnSystem.CanPlayerPlayCard();
    }

    public void UpdateEndTurnButtonState()
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = CanEndPlayerTurn();
        }

        if (instantWinButton != null)
        {
            instantWinButton.interactable = !hasResolvedBattleResult && CurrentTurnState != BattleTurnState.CombatEnd;
        }
    }

    public void RefreshHandPlayableState()
    {
        if (handManager == null)
            return;

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
            return battleBuffController.CanPlayCard(card);
        }

        return card.CanBePlayed();
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        return battleBuffController != null && battleBuffController.ShouldPotionGoToDiscardInsteadOfExhaust(card);
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        return battleBuffController != null && battleBuffController.ShouldExhaustUnlockedUnplayableCard(card);
    }

    public bool CanDrawCards()
    {
        return battleBuffController == null || battleBuffController.CanDrawCards();
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
        return battleBuffController == null || battleBuffController.CanGainCardsToHand();
    }

    public bool CanGainCardsToHandFrom(MoveZoneType from, string subject = null)
    {
        return battleBuffController == null || battleBuffController.CanGainCardsToHandFrom(from, subject);
    }

    public int GetEffectiveCardCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        return battleBuffController != null ? battleBuffController.GetEffectiveCardCost(card) : card.cost;
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

        bool usedEnergy = playerData.UseEnergy(effectiveCost);
        if (usedEnergy)
        {
            battleContext?.OnEnergySpent(effectiveCost);
        }

        return usedEnergy;
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return battleBuffController != null ? battleBuffController.GetCardUseAllEnemiesDamage() : 0;
    }

    public void ApplyCardUseAllEnemiesDamage(int damage)
    {
        battleBuffController?.ApplyCardUseAllEnemiesDamage(damage);
    }

    public int GetAdditionalBarrierGain()
    {
        return battleBuffController != null ? battleBuffController.GetAdditionalBarrierGain() : 0;
    }

    public int ResolvePlayerBarrierGain(int amount)
    {
        return battleBuffController != null ? battleBuffController.ResolvePlayerBarrierGain(amount) : Mathf.Max(0, amount);
    }

    public bool ShouldRetainPlayerBarrierOnTurnStart()
    {
        return battleBuffController != null && battleBuffController.ShouldRetainPlayerBarrierOnTurnStart();
    }

    public int GetPlayerTurnStartBarrierLoss(int currentDefense)
    {
        return battleBuffController != null ? battleBuffController.GetPlayerTurnStartBarrierLoss(currentDefense) : Mathf.Max(0, currentDefense);
    }

    public int GetTurnEndRetainCount()
    {
        return battleBuffController != null ? battleBuffController.GetTurnEndRetainCount() : 0;
    }

    public int GetCardBaseDamageBonus(Card sourceCard, bool isAttackEffect)
    {
        return battleBuffController != null
            ? battleBuffController.GetCardBaseDamageBonus(sourceCard, isAttackEffect)
            : 0;
    }

    public int ApplyCardDamageRuntimeModifiers(Card sourceCard, int damage)
    {
        return battleBuffController != null
            ? battleBuffController.ApplyCardDamageRuntimeModifiers(sourceCard, damage)
            : Mathf.Max(0, damage);
    }

    public int GetPlayerCalculatedCardDamageBonus(float strengthMultiplier)
    {
        return battleBuffController != null ? battleBuffController.GetPlayerCalculatedCardDamageBonus(strengthMultiplier) : 0;
    }

    public float GetPlayerCalculatedCardBaseMultiplier(float baseMultiplier)
    {
        return battleBuffController != null ? battleBuffController.GetPlayerCalculatedCardBaseMultiplier(baseMultiplier) : Mathf.Max(0f, baseMultiplier);
    }

    public float GetPlayerOutgoingDamageMultiplier()
    {
        return battleBuffController != null ? battleBuffController.GetPlayerOutgoingDamageMultiplier() : 1f;
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
        return battleBuffController != null && battleBuffController.TryPreventPlayerIncomingDamage(damage, attacker);
    }

    public void ConsumePlayerIncomingDamageBuffs(Monster attacker, int damage)
    {
        battleBuffController?.ConsumePlayerIncomingDamageBuffs(attacker, damage);
    }

    public int ResolvePersistentUpgradeCardId(int cardId)
    {
        return battleBuffController != null
            ? battleBuffController.ResolvePersistentUpgradeCardId(cardId)
            : cardId;
    }

    public bool HasPermanentBarrierRetention()
    {
        return battleBuffController != null && battleBuffController.HasPermanentBarrierRetention();
    }

    public void HandlePlayedCardPowerEffects(Card playedCard, Monster originalTarget, bool isRepeatedEffect)
    {
        battleBuffController?.HandlePlayedCardEffects(playedCard, originalTarget, isRepeatedEffect);
    }

    public void ResolveDeferredTurnStartPowerEffects()
    {
        battleBuffController?.ResolveDeferredTurnStartEffects();
    }

    public int ConsumeRepeatedPlayCount(Card playedCard, bool isRepeatedEffect)
    {
        return battleBuffController != null ? battleBuffController.ConsumeRepeatedPlayCount(playedCard, isRepeatedEffect) : 0;
    }

    public void HandlePlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        battleBuffController?.HandlePlayerAttackResolved(targetMonster, barrierBefore, barrierAfter);
    }

    public void HandlePlayerHit(Monster attacker, int blockedDamage, int hpDamage)
    {
        battleBuffController?.HandlePlayerHit(attacker, blockedDamage, hpDamage);
    }

    public void HandlePlayerBarrierReduced(int reducedAmount)
    {
        battleBuffController?.HandlePlayerBarrierReduced(reducedAmount);
    }

    public void HandleMonsterHpLost(Monster monster, int hpLoss)
    {
        battleBuffController?.HandleMonsterHpLost(monster, hpLoss);
    }

    public void HandlePlayerHpLost(int hpLoss)
    {
        battleBuffController?.HandlePlayerHpLost(hpLoss);
    }

    public void HandlePlayerDamageDealt(Monster monster, int dealtDamage)
    {
        battleBuffController?.HandlePlayerDamageDealt(monster, dealtDamage);
    }

    public void HandleMonsterBuffApplied(Monster monster, int buffId, int amount)
    {
        battleBuffController?.HandleMonsterBuffApplied(monster, buffId, amount);
    }

    public void ApplyMonsterTurnStartEffects(Monster monster)
    {
        battleBuffController?.ApplyMonsterTurnStartEffects(monster);
    }

    public void ApplyMonsterTurnEndEffects(Monster monster)
    {
        battleBuffController?.ApplyMonsterTurnEndEffects(monster);
    }

    public bool ShouldKeepMonsterBarrierOnTurnStart(Monster monster)
    {
        return battleBuffController != null && battleBuffController.ShouldKeepMonsterBarrierOnTurnStart(monster);
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
        battleBuffController?.HandleMonsterDefenseChanged(monster, previousDefense, currentDefense);
    }

    public int ResolveMonsterBarrierGain(Monster monster, int amount)
    {
        return battleBuffController != null ? battleBuffController.ResolveMonsterBarrierGain(monster, amount) : Mathf.Max(0, amount);
    }

    public int ModifyMonsterOutgoingDamage(Monster monster, int damage)
    {
        return battleBuffController != null ? battleBuffController.ModifyMonsterOutgoingDamage(monster, damage) : Mathf.Max(0, damage);
    }

    public void HandleMonsterAttackResolved(Monster monster, PlayerData target, int attemptedDamage, int hpDamage)
    {
        battleBuffController?.HandleMonsterAttackResolved(monster, target, attemptedDamage, hpDamage);
    }

    public void HandleMonsterBeforeTakeDamage(Monster monster, int incomingDamage)
    {
        battleBuffController?.HandleMonsterBeforeTakeDamage(monster, incomingDamage);
    }

    public void HandleMonsterAfterTakeDamage(Monster monster, int incomingDamage, int damageAfterDefense)
    {
        battleBuffController?.HandleMonsterAfterTakeDamage(monster, incomingDamage, damageAfterDefense);
    }

    public bool CanMonsterReceiveDamage(Monster monster, int incomingDamage)
    {
        return battleBuffController == null || battleBuffController.CanMonsterReceiveDamage(monster, incomingDamage);
    }

    public void HandleCardsExhausted(int count)
    {
        if (count <= 0)
        {
            return;
        }

        battleContext?.OnCardsExhausted(count);
        battleBuffController?.HandleCardsExhausted(count);
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

    public int ReplayExhaustedFeathers()
    {
        return battleBuffController != null ? battleBuffController.ReplayExhaustedFeathers() : 0;
    }

    public bool TryConsumeSoulProtection()
    {
        return battleBuffController != null && battleBuffController.TryConsumeSoulProtection();
    }

    public void HandleEnemyDebuffApplied(Monster monster, int buffId, int amount, int crueltyStackBeforeApply = -1)
    {
        if (BuffData.IsBeneficialBuffId(buffId))
        {
            return;
        }

        battleContext?.OnEnemyDebuffApplied(buffId);
        battleBuffController?.HandleEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
    }

    public void HandleMonsterDeath(Monster monster)
    {
        battleBuffController?.HandleMonsterDeath(monster);

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

        HandleMonsterUnavailableForSelection(monster);

        UnregisterMonster(monster);

        if (monster.gameObject.activeSelf)
        {
            monster.gameObject.SetActive(false);
        }

        UpdateAllUI();
    }

    public void HandleMonsterLeaveCombat(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        battleBuffController?.HandleMonsterLeaveCombat(monster);

        if (currentTarget == monster)
        {
            currentTarget = null;
        }

        if (previewDescriptionTarget == monster)
        {
            previewDescriptionTarget = null;
        }

        HandleMonsterUnavailableForSelection(monster);

        UnregisterMonster(monster);

        if (monster.gameObject.activeSelf)
        {
            monster.gameObject.SetActive(false);
        }

        UpdateAllUI();
    }

    public void HandleMonsterRevived(Monster monster)
    {
        battleBuffController?.HandleMonsterRevived(monster);
    }

    public bool CanMonsterRevive(Monster monster)
    {
        return battleBuffController == null || battleBuffController.CanMonsterRevive(monster);
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

    public Card ApplyPersistentUpgradeToCard(Card card)
    {
        return ApplyPersistentUpgradeToCard(card, 0);
    }

    public Card ApplyPersistentUpgradeToCard(Card card, int sourceBuffId)
    {
        if (card == null || battleBuffController == null)
        {
            return card;
        }

        int upgradedCardId = sourceBuffId > 0
            ? battleBuffController.ResolvePersistentUpgradeCardId(card.cardId, sourceBuffId)
            : battleBuffController.ResolvePersistentUpgradeCardId(card.cardId);
        if (upgradedCardId == card.cardId)
        {
            return card;
        }

        Card upgradedTemplate = CardManager.GetCardAsCard(upgradedCardId);
        if (upgradedTemplate == null)
        {
            return card;
        }

        card.ApplyTemplate(upgradedTemplate);
        return card;
    }

    public void ApplyBuffToPlayer(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (battleBuffController != null && battleBuffController.TryApplyToPlayer(buffId, amount))
        {
            return;
        }

        playerData?.AddBuff(buffId, amount);
        battleBuffController?.HandleBuffApplied(buffId);
    }

    public void ApplyBuffToMonster(Monster monster, int buffId, int amount)
    {
        if (monster == null || monster.IsDead() || amount <= 0)
        {
            return;
        }

        buffId = battleBuffController != null
            ? battleBuffController.ResolveAppliedMonsterBuffId(monster, buffId, amount)
            : buffId;

        if (battleBuffController != null && battleBuffController.TryApplyToMonster(buffId, monster, amount))
        {
            return;
        }

        int crueltyStackBeforeApply = monster.GetBuffStack(BattleRuntimeDefinitions.CrueltyDebuffId);
        monster.AddBuff(buffId, amount);
        battleBuffController?.HandleBuffApplied(buffId);
        if (!BuffData.IsBeneficialBuffId(buffId))
        {
            HandleEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
        }
    }

    public void ApplyBuffToAllEnemies(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (battleBuffController != null && battleBuffController.TryApplyToAllEnemies(buffId, amount))
        {
            return;
        }

        List<Monster> targets = GetLivingMonsters();
        foreach (Monster monster in targets)
        {
            ApplyBuffToMonster(monster, buffId, amount);
        }
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

    public void SetState(BattleTurnState newState)
    {
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
        if (battleDeckViewer == null)
        {
            Debug.LogWarning("[BattleManager] BattleDeckViewer is missing. Fallback to auto selection.");
            return false;
        }

        return battleDeckViewer.OpenSelectionPanel(selectableCards, selectCount, onSelected, allowFewer);
    }

    public bool OpenMonsterSelection(List<Monster> selectionTargets, int selectCount, Action<List<Monster>> onSelected)
    {
        if (selectionTargets == null || selectCount <= 0 || onSelected == null)
        {
            return false;
        }

        CancelMonsterSelection();

        foreach (Monster monster in selectionTargets)
        {
            if (monster == null || monster.IsDead() || selectableMonsters.Contains(monster))
            {
                continue;
            }

            selectableMonsters.Add(monster);
        }

        if (selectableMonsters.Count < selectCount)
        {
            selectableMonsters.Clear();
            return false;
        }

        requiredMonsterSelectionCount = selectCount;
        pendingMonsterSelectionCallback = onSelected;
        isMonsterSelectionActive = true;
        RefreshHandPlayableState();
        UpdateEndTurnButtonState();
        return true;
    }

    public void HandleMonsterClicked(Monster monster)
    {
        if (!isMonsterSelectionActive || monster == null || monster.IsDead() || !selectableMonsters.Contains(monster))
        {
            return;
        }

        if (selectedMonsters.Contains(monster))
        {
            selectedMonsters.Remove(monster);
            monster.SetSelectionHighlight(false);
            return;
        }

        if (selectedMonsters.Count >= requiredMonsterSelectionCount)
        {
            return;
        }

        selectedMonsters.Add(monster);
        monster.SetSelectionHighlight(true);

        if (selectedMonsters.Count >= requiredMonsterSelectionCount)
        {
            CompleteMonsterSelection();
        }
    }

    public void RegisterMonsterHpLossHealPlayerThisTurn(Monster monster)
    {
        battleBuffController?.RegisterMonsterHpLossHealPlayerThisTurn(monster);
    }

    private void CompleteMonsterSelection()
    {
        List<Monster> resolvedSelection = new List<Monster>(selectedMonsters);
        Action<List<Monster>> callback = pendingMonsterSelectionCallback;
        ClearMonsterSelectionState();
        callback?.Invoke(resolvedSelection);
    }

    private void CancelMonsterSelection()
    {
        if (!isMonsterSelectionActive && selectableMonsters.Count == 0 && selectedMonsters.Count == 0)
        {
            return;
        }

        ClearMonsterSelectionState();
    }

    private void ClearMonsterSelectionState()
    {
        foreach (Monster selectedMonster in selectedMonsters)
        {
            selectedMonster?.SetSelectionHighlight(false);
        }

        selectableMonsters.Clear();
        selectedMonsters.Clear();
        pendingMonsterSelectionCallback = null;
        requiredMonsterSelectionCount = 0;
        isMonsterSelectionActive = false;
        RefreshHandPlayableState();
        UpdateEndTurnButtonState();
    }

    private void HandleMonsterUnavailableForSelection(Monster monster)
    {
        if (monster == null)
        {
            return;
        }

        monster.SetSelectionHighlight(false);
        selectableMonsters.Remove(monster);
        selectedMonsters.Remove(monster);

        if (isMonsterSelectionActive && selectableMonsters.Count < requiredMonsterSelectionCount)
        {
            CancelMonsterSelection();
        }
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

            Card drawnCard = drawCard();
            if (drawnCard == null)
            {
                break;
            }

            drawnCards.Add(drawnCard);
            handManager.AddCard(drawnCard);
            drawnCard.ExecuteOnDrawEffects(this);

            if (!ignoreRootAbsorption && drawnCard.cardId == 40)
            {
                break;
            }
        }

        if (battleContext != null && drawnCards.Count > 0)
        {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        return drawnCards;
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
        if (battleResultTransitionDelay > 0f)
        {
            yield return new WaitForSeconds(battleResultTransitionDelay);
        }

        if (!TrainingRunState.HasMapData)
            yield break;

        bool shouldPersistRunDeck = false;
        if (isVictory
            && TrainingRunState.PendingNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode))
        {
            shouldPersistRunDeck = pendingNode.nodeType == TrainingNodeType.Boss;
        }

        if (PlayerData.Instance != null)
        {
            TrainingRunState.SetPlayerHealthState(PlayerData.Instance.hp, PlayerData.Instance.maxHP);
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

        if (!string.IsNullOrEmpty(TrainingRunState.MapSceneName))
        {
            SceneManager.LoadScene(TrainingRunState.MapSceneName);
        }
    }
}

