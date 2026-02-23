using System.Collections;
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
/// Training mode battle manager.
/// Selects 3 characters, composes one battle deck, and runs turn-based combat.
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

    [Header("Turn Settings")]
    public int drawCardCount = 6;
    [SerializeField] private int playerBaseEnergyPerTurn = 3;
    [SerializeField] private float enemyActionDelay = 0.2f;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private bool enableKeyboardEndTurn = true;

    [Header("Spawned Monsters")]
    public List<Monster> spawnedMonsters = new List<Monster>();

    [Header("Debug")]
    public bool isDebugMode = false;
    public EffectType debugTargetEffect = EffectType.Barrier;

    // Temporary target used while card effects are executing.
    public Monster currentTarget;

    [Header("Run Data")]
    public static BuildingDeck buildingDeck;

    public BattleTurnState CurrentTurnState { get; private set; } = BattleTurnState.None;

    private bool isTurnTransitioning;
    private int turnNumber;
    private int pendingExtraDrawAtTurnStart;

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

        if (BuffManager.Instance == null)
        {
            Debug.Log("[BattleManager] BuffManager missing. Creating runtime instance.");
            GameObject go = new GameObject("BuffManager");
            go.AddComponent<BuffManager>();
        }

        if (buildingDeck == null)
        {
            Debug.Log("[BattleManager] New run started. Creating BuildingDeck.");
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

        List<Card> battleDeck = buildingDeck.CopyDeck();
        usableDeckManager.SetDeck(battleDeck);

        UpdateAllUI();
    }

    private void StartGame()
    {
        if (!ValidateRuntimeReferences())
            return;

        SetState(BattleTurnState.CombatStart);
        turnNumber = 0;
        pendingExtraDrawAtTurnStart = 0;
        isTurnTransitioning = false;

        battleContext.OnCombatStart();
        usableDeckManager.ShuffleDeck();

        ApplyCombatStartEffects();

        if (TryHandleCombatEnd())
            return;

        BeginPlayerTurn();
    }

    public void DrawCards(int count)
    {
        if (count <= 0 || usableDeckManager == null || handManager == null)
            return;

        List<Card> drawnCards = usableDeckManager.DrawCard(count);
        handManager.AddCard(drawnCards);

        if (battleContext != null)
        {
            battleContext.cardsDrawnThisTurn += drawnCards.Count;
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    [ContextMenu("Debug Draw Cards By Effect")]
    public void DebugDrawSpecificEffectCard()
    {
        if (!isDebugMode || usableDeckManager == null || handManager == null || usableDeckManager.usableDeck == null)
            return;

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
                    }

                    if (isMatch)
                        break;
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

        usableDeckManager.usableDeck = remainingDeck;

        if (matchingCards.Count > 0)
        {
            Debug.Log($"[Debug] Added {matchingCards.Count} cards with {debugTargetEffect} effect to hand.");
            handManager.AddCard(matchingCards);
            if (battleContext != null)
            {
                battleContext.cardsDrawnThisTurn += matchingCards.Count;
            }
        }
        else
        {
            Debug.LogWarning($"[Debug] No cards with {debugTargetEffect} effect found in deck.");
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    private void HandleCardClicked(CardPlayEventData eventData)
    {
        PlayCard(eventData);
    }

    private void PlayCard(CardPlayEventData eventData)
    {
        if (!CanPlayerPlayCard())
        {
            Debug.LogWarning("[BattleManager] Cannot play card right now. Not in player action state.");
            return;
        }

        if (eventData == null || eventData.cardController == null || eventData.cardController.Card == null)
        {
            Debug.LogWarning("[BattleManager] Missing CardController or Card.");
            return;
        }

        CardController controller = eventData.cardController;
        Card playedCard = controller.Card;

        if (playerData == null)
            return;

        if (playerData.energy < playedCard.cost)
        {
            Debug.LogWarning($"[BattleManager] Not enough energy for {playedCard.cardName}. Needed: {playedCard.cost}, Current: {playerData.energy}");
            RefreshHandPlayableState();
            UpdateAllUI();
            return;
        }

        bool spent = playerData.UseEnergy(playedCard.cost);
        if (!spent)
        {
            RefreshHandPlayableState();
            UpdateAllUI();
            return;
        }

        Debug.Log($"[Player] Used card: {playedCard.cardName} (Energy now: {playerData.energy})");

        battleContext?.OnCardPlayed(playedCard);

        currentTarget = eventData.targetMonster;
        playedCard.Play(this);
        currentTarget = null;

        if (handManager != null)
        {
            handManager.RemoveCardFromHand(controller.cardUI);
        }

        if (usableDeckManager != null)
        {
            usableDeckManager.AddToDiscard(playedCard);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
        TryHandleCombatEnd();
    }

    public void EndTurn()
    {
        if (!CanEndPlayerTurn())
        {
            Debug.LogWarning("[BattleManager] EndTurn ignored. It is not the player's actionable state.");
            return;
        }

        StartCoroutine(RunEnemyTurnSequence());
    }

    /// <summary>
    /// Allows card effects/boss rules to force end the player's turn.
    /// </summary>
    public void ForceEndPlayerTurn()
    {
        if (CurrentTurnState == BattleTurnState.CombatEnd || isTurnTransitioning)
            return;

        if (CurrentTurnState == BattleTurnState.PlayerTurnStart ||
            CurrentTurnState == BattleTurnState.PlayerAction ||
            CurrentTurnState == BattleTurnState.PlayerTurnEnd)
        {
            StartCoroutine(RunEnemyTurnSequence());
        }
    }

    private IEnumerator RunEnemyTurnSequence()
    {
        if (isTurnTransitioning)
            yield break;

        isTurnTransitioning = true;

        SetState(BattleTurnState.PlayerTurnEnd);
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();

        ApplyPlayerTurnEndEffects();
        DiscardRemainingHandCards();

        UpdateAllUI();

        if (TryHandleCombatEnd())
        {
            isTurnTransitioning = false;
            yield break;
        }

        SetState(BattleTurnState.EnemyTurnStart);
        foreach (Monster monster in GetLivingMonsters())
        {
            monster.OnTurnStart();
        }

        yield return new WaitForSeconds(enemyActionDelay);

        SetState(BattleTurnState.EnemyAction);

        List<Monster> enemiesForAction = GetLivingMonsters();
        foreach (Monster monster in enemiesForAction)
        {
            if (monster == null || monster.IsDead())
                continue;

            monster.ExecutePlannedAction(playerData);
            UpdateAllUI();

            if (playerData != null && playerData.IsDead())
                break;

            yield return new WaitForSeconds(enemyActionDelay);
        }

        if (TryHandleCombatEnd())
        {
            isTurnTransitioning = false;
            yield break;
        }

        SetState(BattleTurnState.EnemyTurnEnd);
        foreach (Monster monster in GetLivingMonsters())
        {
            monster.OnTurnEnd();
        }

        yield return new WaitForSeconds(enemyActionDelay);

        if (TryHandleCombatEnd())
        {
            isTurnTransitioning = false;
            yield break;
        }

        isTurnTransitioning = false;
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn()
    {
        if (CurrentTurnState == BattleTurnState.CombatEnd)
            return;

        turnNumber++;
        SetState(BattleTurnState.PlayerTurnStart);

        if (battleContext == null)
        {
            battleContext = new BattleContext();
        }
        battleContext.OnTurnStart();

        if (playerData != null)
        {
            playerData.OnTurnStart();
        }

        ApplyPlayerTurnStartEffects();
        PlanEnemyNextActions();

        if (isDebugMode && turnNumber == 1)
        {
            DebugDrawSpecificEffectCard();
        }
        else
        {
            DrawCards(GetTurnStartDrawCount());
        }

        SetState(BattleTurnState.PlayerAction);
        UpdateEndTurnButtonState();
        RefreshHandPlayableState();
        UpdateAllUI();

        Debug.Log($"[BattleManager] Player turn started. Turn: {turnNumber}");
    }

    private int GetTurnStartDrawCount()
    {
        int total = Mathf.Max(0, drawCardCount + pendingExtraDrawAtTurnStart);
        pendingExtraDrawAtTurnStart = 0;
        return total;
    }

    /// <summary>
    /// Extension point for effects that increase/decrease next turn draw.
    /// </summary>
    public void AddTurnStartDrawModifier(int amount)
    {
        pendingExtraDrawAtTurnStart += amount;
    }

    private void ApplyCombatStartEffects()
    {
        // Placeholder: start-of-combat buffs/debuffs can be resolved here.
    }

    private void ApplyPlayerTurnStartEffects()
    {
        // Placeholder: player turn-start trigger effects.
    }

    private void ApplyPlayerTurnEndEffects()
    {
        if (playerData != null)
        {
            playerData.OnTurnEnd();
        }

        // Placeholder: player turn-end trigger effects.
    }

    private void PlanEnemyNextActions()
    {
        foreach (Monster monster in GetLivingMonsters())
        {
            monster.PlanNextAction();
        }
    }

    private void DiscardRemainingHandCards()
    {
        if (handManager == null || usableDeckManager == null)
            return;

        List<Card> remainingCards = handManager.GetHandCards();
        if (remainingCards.Count > 0)
        {
            usableDeckManager.AddToDiscard(remainingCards);
            if (battleContext != null)
            {
                battleContext.cardsDiscardedThisTurn += remainingCards.Count;
            }
        }

        handManager.ClearHand();
    }

    private List<Monster> GetLivingMonsters()
    {
        CleanupMonsterList();

        List<Monster> alive = new List<Monster>();
        foreach (Monster monster in spawnedMonsters)
        {
            if (monster != null && !monster.IsDead())
            {
                alive.Add(monster);
            }
        }

        return alive;
    }

    private void CleanupMonsterList()
    {
        spawnedMonsters.RemoveAll(monster => monster == null);
    }

    private bool TryHandleCombatEnd()
    {
        if (CurrentTurnState == BattleTurnState.CombatEnd)
            return true;

        if (playerData != null && playerData.IsDead())
        {
            SetState(BattleTurnState.CombatEnd);
            UpdateEndTurnButtonState();
            RefreshHandPlayableState();
            UpdateAllUI();
            Debug.Log("[BattleManager] Defeat. Player is dead.");
            return true;
        }

        if (GetLivingMonsters().Count == 0)
        {
            SetState(BattleTurnState.CombatEnd);
            UpdateEndTurnButtonState();
            RefreshHandPlayableState();
            UpdateAllUI();
            Debug.Log("[BattleManager] Victory. All enemies are dead.");
            return true;
        }

        return false;
    }

    private bool CanPlayerPlayCard()
    {
        return !isTurnTransitioning && CurrentTurnState == BattleTurnState.PlayerAction;
    }

    private bool CanEndPlayerTurn()
    {
        return !isTurnTransitioning && CurrentTurnState == BattleTurnState.PlayerAction;
    }

    public bool CanUseIdentityAbility()
    {
        return !isTurnTransitioning && CurrentTurnState == BattleTurnState.PlayerAction;
    }

    private void UpdateEndTurnButtonState()
    {
        if (endTurnButton == null)
            return;

        endTurnButton.interactable = CanEndPlayerTurn();
    }

    private void RefreshHandPlayableState()
    {
        if (handManager == null)
            return;

        bool canInteract = CanPlayerPlayCard();

        handManager.RefreshCardPlayability(
            card => playerData != null && playerData.energy >= card.cost,
            canInteract);
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

    private void SetState(BattleTurnState newState)
    {
        CurrentTurnState = newState;
    }

    public void UpdateAllUI()
    {
        if (battleUI != null)
            battleUI.UpdateAllUI();
    }
}
