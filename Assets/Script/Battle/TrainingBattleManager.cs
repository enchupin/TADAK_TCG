using System;
using System.Collections.Generic;
using UnityEngine;
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

/// <summary>
/// Training mode battle orchestrator.
/// Responsible for wiring references and delegating combat flow to subsystems.
/// </summary>
public class TrainingBattleManager : MonoBehaviour
{
    public static TrainingBattleManager Instance { get; private set; }

    [Header("Battle Data")]
    public PlayerData playerData;
    public BattleContext battleContext;

    [Header("Manager References")]
    public UsableDeckManager usableDeckManager;
    public HandManager handManager;
    public BattleUI battleUI;
    public MonsterSpawner monsterSpawner;
    [SerializeField] private BattleDeckViewer battleDeckViewer;

    [Header("Turn Settings")]
    public int drawCardCount = 6;
    [SerializeField] private int playerBaseEnergyPerTurn = 3;
    [SerializeField] private float enemyActionDelay = 0.2f;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private bool enableKeyboardEndTurn = true;

    [Header("Training Flow")]
    [SerializeField] private bool enableTrainingRunFlow = true;
    [SerializeField] private float battleResultTransitionDelay = 0.8f;

    [Header("Spawned Monsters")]
    public List<Monster> spawnedMonsters = new List<Monster>();

    [Header("Debug")]
    public bool isDebugMode = false;
    public EffectType debugTargetEffect = EffectType.Barrier;
    [SerializeField] private int debugEnergyAmount = 1000;

    // Temporary target used while card effects are executing.
    public Monster currentTarget;

    [Header("Run Data")]
    public static BuildingDeck buildingDeck;

    public BattleTurnState CurrentTurnState { get; private set; } = BattleTurnState.None;

    private TurnSystem turnSystem;
    private CombatResolver combatResolver;
    private EncounterSystem encounterSystem;
    private PowerBuffRuntime powerBuffRuntime;
    private bool hasResolvedBattleResult;

    public float EnemyActionDelay => enemyActionDelay;

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
        powerBuffRuntime = new PowerBuffRuntime(this);

        if (battleDeckViewer == null)
        {
            battleDeckViewer = FindAnyObjectByType<BattleDeckViewer>();
        }
    }

    private void Start()
    {
        CardPlayEvents.OnCardPlayed += HandleCardClicked;

        InitializeCharacterSelection();
        InitializeBattle();

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

        if (BuffManager.Instance == null)
        {
            Debug.Log("[BattleManager] BuffManager missing. Creating runtime instance.");
            GameObject go = new GameObject("BuffManager");
            go.AddComponent<BuffManager>();
        }

        if (monsterSpawner != null)
        {
            Monster spawned = monsterSpawner.SpawnMonster();
            if (spawned != null)
            {
                RegisterMonster(spawned);
            }
        }
        else
        {
            Debug.LogWarning("[BattleManager] MonsterSpawner is not assigned.");
        }

        battleContext = new BattleContext();

        if (!CardManager.IsInitialized())
        {
            Debug.LogError("[BattleManager] CardManager is not initialized.");
            return;
        }

        bool shouldRebuildDeck = buildingDeck == null || !TrainingRunState.IsRunActive;
        if (shouldRebuildDeck)
        {
            Debug.Log("[BattleManager] Rebuilding BuildingDeck from current character selection.");
            buildingDeck = new BuildingDeck();
            buildingDeck.Initialize(SelectedButtonControl.selectedCharacterList);
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
        powerBuffRuntime?.ResetForCombat();
        ApplyCombatStartEffects();

        if (TryHandleCombatEnd())
            return;

        turnSystem.BeginPlayerTurn();
    }

    public void DrawCards(int count)
    {
        DrawCardsAndGet(count);
    }

    public List<Card> DrawCardsAndGet(int count)
    {
        if (count <= 0 || usableDeckManager == null || handManager == null)
            return new List<Card>();

        List<Card> drawnCards = usableDeckManager.DrawCard(count);
        handManager.AddCard(drawnCards);

        if (battleContext != null)
        {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        return drawnCards;
    }

    public void DrawBasicCards(int count, Character? characterFilter = null)
    {
        DrawBasicCardsAndGet(count, characterFilter);
    }

    public List<Card> DrawBasicCardsAndGet(int count, Character? characterFilter = null)
    {
        if (count <= 0 || usableDeckManager == null || handManager == null) {
            return new List<Card>();
        }

        List<Card> drawnCards = usableDeckManager.DrawBasicCards(count, characterFilter);
        handManager.AddCard(drawnCards);

        if (battleContext != null) {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        return drawnCards;
    }

    public void DrawCharacterCards(int count, Character? characterFilter = null)
    {
        DrawCharacterCardsAndGet(count, characterFilter);
    }

    public List<Card> DrawCharacterCardsAndGet(int count, Character? characterFilter = null)
    {
        if (count <= 0 || usableDeckManager == null || handManager == null)
        {
            return new List<Card>();
        }

        List<Card> drawnCards = usableDeckManager.DrawCharacterCards(count, characterFilter);
        handManager.AddCard(drawnCards);

        if (battleContext != null)
        {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        return drawnCards;
    }

    [ContextMenu("Debug Draw Cards By Effect")]
    public void DebugDrawCardsByEffect()
    {
        if (!isDebugMode || handManager == null) {
            return;
        }

        List<Card> matchingCards = new List<Card>();
        List<CardData> allCardData = CardManager.GetAllCards();
        foreach (CardData cardData in allCardData) {
            if (cardData == null) {
                continue;
            }

            Card card = cardData.ToCard();
            if (card == null) {
                continue;
            }

            if (!CardHasDebugTargetEffect(card)) {
                continue;
            }

            matchingCards.Add(card);
        }

        if (matchingCards.Count > 0) {
            Debug.Log($"[Debug] Added {matchingCards.Count} cards with {debugTargetEffect} effect from CardManager.");
            handManager.AddCard(matchingCards);
            if (battleContext != null) {
                battleContext.OnCardsDrawn(matchingCards.Count);
            }
        }
        else {
            Debug.LogWarning($"[Debug] No cards with {debugTargetEffect} effect found in CardManager cache. 카드 JSON 변환/카드 컬렉션 갱신 여부를 확인하세요.");
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    private bool CardHasDebugTargetEffect(Card card)
    {
        if (card == null)
            return false;

        if (card.effects != null)
        {
            foreach (ICardEffect effect in card.effects)
            {
                if (EffectMatchesDebugTarget(effect))
                    return true;
            }
        }

        if (card.keepEffects != null)
        {
            foreach (ICardEffect effect in card.keepEffects)
            {
                if (EffectMatchesDebugTarget(effect))
                    return true;
            }
        }

        return false;
    }

    private bool EffectMatchesDebugTarget(ICardEffect effect)
    {
        if (effect == null)
            return false;

        switch (debugTargetEffect)
        {
            case EffectType.Barrier:
                if (effect is BarrierEffect) return true;
                break;
            case EffectType.Damage:
                if (effect is DamageEffect) return true;
                break;
            case EffectType.Draw:
                if (effect is DrawEffect) return true;
                break;
            case EffectType.DrawCharacter:
                if (effect is DrawCharacterEffect) return true;
                break;
            case EffectType.DrawBasic:
                if (effect is DrawBasicEffect) return true;
                break;
            case EffectType.RandGenerate:
                if (effect is RandGenerateEffect) return true;
                break;
            case EffectType.Move:
                if (effect is MoveEffect) return true;
                break;
            case EffectType.ExhaustCard:
                if (effect is ExhaustCardEffect) return true;
                break;
            case EffectType.Repeat:
                if (effect is RepeatEffect) return true;
                break;
            case EffectType.Trigger:
                if (effect is TriggerEffect) return true;
                break;
            case EffectType.Kill:
                if (effect is KillEffect) return true;
                break;
            case EffectType.ChangeStat:
                if (effect is ChangeStatEffect) return true;
                break;
            case EffectType.ReduceCost:
                if (effect is ReduceCostEffect) return true;
                break;
            case EffectType.Cost:
                if (effect is CostEffect) return true;
                break;
            case EffectType.ModifyCard:
                if (effect is ModifyCardEffect) return true;
                break;
            case EffectType.ModifyCards:
                if (effect is ModifyCardsEffect) return true;
                break;
            case EffectType.Upgrade:
                if (effect is UpgradeEffect) return true;
                break;
            case EffectType.ExtraTurn:
                if (effect is ExtraTurnEffect) return true;
                break;
            case EffectType.MixBuff:
                if (effect is MixBuffEffect) return true;
                break;
            case EffectType.RemoveBuff:
                if (effect is RemoveBuffEffect) return true;
                break;
            case EffectType.MultiplyBarrier:
                if (effect is MultiplyBarrierEffect) return true;
                break;
            case EffectType.Stamina:
                if (effect is StaminaEffect) return true;
                break;
            case EffectType.Attack:
                if (effect is AttackEffect) return true;
                break;
            case EffectType.Heal:
                if (effect is HealEffect) return true;
                break;
            case EffectType.Buff:
                if (effect is BuffEffect) return true;
                break;
            case EffectType.Scry:
                if (effect is ScryEffect) return true;
                break;
        }

        if (effect is AttackEffect attack && attack.onActions != null)
        {
            foreach (ICardEffect nested in attack.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is DamageEffect damage && damage.onActions != null)
        {
            foreach (ICardEffect nested in damage.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is DrawEffect draw && draw.onActions != null)
        {
            foreach (ICardEffect nested in draw.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is DrawBasicEffect drawBasic && drawBasic.onActions != null)
        {
            foreach (ICardEffect nested in drawBasic.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is DrawCharacterEffect drawCharacter && drawCharacter.onActions != null)
        {
            foreach (ICardEffect nested in drawCharacter.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is BarrierEffect barrier && barrier.onActions != null)
        {
            foreach (ICardEffect nested in barrier.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is ExhaustCardEffect exhaust && exhaust.onActions != null)
        {
            foreach (ICardEffect nested in exhaust.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is CopyEffect copy && copy.onActions != null)
        {
            foreach (ICardEffect nested in copy.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is MoveEffect move && move.onActions != null)
        {
            foreach (ICardEffect nested in move.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is SelectCardEffect selectCard && selectCard.onActions != null)
        {
            foreach (ICardEffect nested in selectCard.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is RepeatEffect repeat && repeat.effectToRepeat != null)
        {
            if (EffectMatchesDebugTarget(repeat.effectToRepeat))
                return true;
        }
        if (effect is ConditionalEffect conditional)
        {
            if (conditional.successEffects != null)
            {
                foreach (ICardEffect nested in conditional.successEffects)
                {
                    if (EffectMatchesDebugTarget(nested))
                        return true;
                }
            }

            if (conditional.failEffects != null)
            {
                foreach (ICardEffect nested in conditional.failEffects)
                {
                    if (EffectMatchesDebugTarget(nested))
                        return true;
                }
            }
        }
        if (effect is ChangeStatEffect changeStat && changeStat.onActions != null)
        {
            foreach (ICardEffect nested in changeStat.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }

        return false;
    }

    private void HandleCardClicked(CardPlayEventData eventData)
    {
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

    public void ApplyCombatStartEffects()
    {
        if (usableDeckManager == null || handManager == null)
        {
            return;
        }
        List<Card> openingCards = new List<Card>();
        List<Card> drawPile = usableDeckManager.GetDrawPile();
        foreach (Card card in drawPile)
        {
            if (card == null || !card.HasKeyword(CardKeywordIds.Opening))
            {
                continue;
            }
            if (usableDeckManager.RemoveFromDrawPile(card))
            {
                openingCards.Add(card);
            }
        }
        if (openingCards.Count <= 0)
        {
            return;
        }
        handManager.AddCard(openingCards);
        battleContext?.OnCardsDrawn(openingCards.Count);
        RefreshHandPlayableState();
        UpdateAllUI();
    }

    public void ApplyPlayerTurnStartEffects()
    {
        ApplyDebugEnergy();
        powerBuffRuntime?.OnTurnStart();
    }

    public void ApplyPlayerTurnEndEffects()
    {
        if (playerData != null)
        {
            playerData.OnTurnEnd();
        }

        powerBuffRuntime?.OnTurnEnd();
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
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        UpdateAllUI();

        Debug.Log(isVictory
            ? "[BattleManager] Victory. All enemies are dead."
            : "[BattleManager] Defeat. Player is dead.");

        if (enableTrainingRunFlow && TrainingRunState.IsRunActive)
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
        return turnSystem.CanPlayerPlayCard();
    }

    public bool CanEndPlayerTurn()
    {
        return turnSystem.CanEndPlayerTurn();
    }

    public bool CanUseIdentityAbility()
    {
        return turnSystem.CanPlayerPlayCard();
    }

    public void UpdateEndTurnButtonState()
    {
        if (endTurnButton == null)
            return;

        endTurnButton.interactable = CanEndPlayerTurn();
    }

    public void RefreshHandPlayableState()
    {
        if (handManager == null)
            return;

        bool canInteract = CanPlayerPlayCard();

        handManager.RefreshCardPlayability(
            card => playerData != null && card != null && CanPlayCard(card) && playerData.energy >= card.cost,
            canInteract);
    }

    public bool CanPlayCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        if (powerBuffRuntime != null)
        {
            return powerBuffRuntime.CanPlayCard(card);
        }

        return card.CanBePlayed();
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        return powerBuffRuntime != null && powerBuffRuntime.ShouldPotionGoToDiscardInsteadOfExhaust(card);
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        return powerBuffRuntime != null && powerBuffRuntime.ShouldExhaustUnlockedUnplayableCard(card);
    }

    public int GetAdditionalBarrierGain()
    {
        return powerBuffRuntime != null ? powerBuffRuntime.GetAdditionalBarrierGain() : 0;
    }

    public int GetTurnEndRetainCount()
    {
        return powerBuffRuntime != null ? powerBuffRuntime.GetTurnEndRetainCount() : 0;
    }

    public bool HasPermanentBarrierRetention()
    {
        return powerBuffRuntime != null && powerBuffRuntime.HasPermanentBarrierRetention();
    }

    public void HandlePlayedCardPowerEffects(Card playedCard, Monster originalTarget, bool isRepeatedEffect)
    {
        powerBuffRuntime?.OnCardPlayed(playedCard, originalTarget, isRepeatedEffect);
    }

    public int ConsumeRepeatedPlayCount(Card playedCard, bool isRepeatedEffect)
    {
        return powerBuffRuntime != null ? powerBuffRuntime.ConsumeRepeatCount(playedCard, isRepeatedEffect) : 0;
    }

    public void HandlePlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        powerBuffRuntime?.OnAttackResolved(targetMonster, barrierBefore, barrierAfter);
    }

    public void HandlePlayerHit(Monster attacker, int blockedDamage, int hpDamage)
    {
        powerBuffRuntime?.OnPlayerHit(attacker, blockedDamage, hpDamage);
    }

    public void HandlePlayerBarrierReduced(int reducedAmount)
    {
        powerBuffRuntime?.OnPlayerBarrierReduced(reducedAmount);
    }

    public List<Card> ProcessGeneratedCards(List<Card> generatedCards, bool allowDuplicateGeneration = true)
    {
        if (generatedCards == null)
        {
            return new List<Card>();
        }

        return powerBuffRuntime != null
            ? powerBuffRuntime.ProcessGeneratedCards(generatedCards, allowDuplicateGeneration)
            : new List<Card>(generatedCards);
    }

    public void ApplyBuffToPlayer(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerData?.AddBuff(buffId, amount);
        ApplyPersistentCardBuffChanges(buffId);
    }

    public void ApplyBuffToMonster(Monster monster, int buffId, int amount)
    {
        if (monster == null || monster.IsDead() || amount <= 0)
        {
            return;
        }

        monster.AddBuff(buffId, amount);
        if (!BuffData.IsBeneficialBuffId(buffId))
        {
            powerBuffRuntime?.OnEnemyDebuffApplied(monster, buffId, amount);
        }
    }

    public void ApplyBuffToAllEnemies(int buffId, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        List<Monster> targets = GetLivingMonsters();
        foreach (Monster monster in targets)
        {
            ApplyBuffToMonster(monster, buffId, amount);
        }
    }

    private void ApplyPersistentCardBuffChanges(int buffId)
    {
        if (powerBuffRuntime == null || (buffId != 1002 && buffId != 1004))
        {
            return;
        }

        List<Card> changedHandCards = new List<Card>();
        bool hasChanges = false;

        hasChanges |= ApplyPersistentCardBuffChanges(handManager?.GetHandCards(), changedHandCards);
        hasChanges |= ApplyPersistentCardBuffChanges(usableDeckManager?.GetDrawPile(), null);
        hasChanges |= ApplyPersistentCardBuffChanges(usableDeckManager?.GetDiscardPile(), null);

        if (!hasChanges)
        {
            return;
        }

        if (handManager != null && changedHandCards.Count > 0)
        {
            handManager.RefreshCardDisplays(changedHandCards);
            RefreshHandPlayableState();
        }

        UpdateAllUI();
    }

    private bool ApplyPersistentCardBuffChanges(List<Card> cards, List<Card> changedCards)
    {
        if (cards == null || cards.Count == 0 || powerBuffRuntime == null)
        {
            return false;
        }

        bool hasChanges = false;
        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            int upgradedCardId = powerBuffRuntime.ResolvePersistentUpgradeCardId(card.cardId);
            if (upgradedCardId == card.cardId)
            {
                continue;
            }

            Card upgradedTemplate = CardManager.GetCardAsCard(upgradedCardId);
            if (upgradedTemplate == null)
            {
                continue;
            }

            card.ApplyTemplate(upgradedTemplate);
            hasChanges = true;
            changedCards?.Add(card);
        }

        return hasChanges;
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
        if (battleUI != null)
            battleUI.UpdateAllUI();
    }

    public bool OpenSelectCardPanel(List<Card> selectableCards, int selectCount, Action<List<Card>> onSelected)
    {
        if (battleDeckViewer == null)
        {
            Debug.LogWarning("[BattleManager] BattleDeckViewer is missing. Fallback to auto selection.");
            return false;
        }

        return battleDeckViewer.OpenSelectionPanel(selectableCards, selectCount, onSelected);
    }

    private void ApplyDebugEnergy()
    {
        if (!isDebugMode || playerData == null)
            return;

        playerData.maxEnergy = debugEnergyAmount;
        playerData.energy = debugEnergyAmount;
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

        Debug.Log($"[BattleManager] \uB514\uBC84\uADF8 \uB371 \uAD6C\uC131 \uC644\uB8CC: {debugDeck.Count}\uC7A5");
        return debugDeck;
    }

    private System.Collections.IEnumerator HandleTrainingRunBattleResult(bool isVictory)
    {
        if (battleResultTransitionDelay > 0f)
        {
            yield return new WaitForSeconds(battleResultTransitionDelay);
        }

        TrainingRunSceneActions.HandleBattleFinished(isVictory);
    }
}

