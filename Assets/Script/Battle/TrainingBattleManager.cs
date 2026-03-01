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
        hasResolvedBattleResult = false;

        battleContext.OnCombatStart();
        usableDeckManager.ShuffleDeck();

        turnSystem.ResetForCombat();
        ApplyCombatStartEffects();

        if (TryHandleCombatEnd())
            return;

        turnSystem.BeginPlayerTurn();
    }

    public void DrawCards(int count)
    {
        if (count <= 0 || usableDeckManager == null || handManager == null)
            return;

        List<Card> drawnCards = usableDeckManager.DrawCard(count);
        handManager.AddCard(drawnCards);

        if (battleContext != null)
        {
            battleContext.OnCardsDrawn(drawnCards.Count);
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    [ContextMenu("Debug Draw Cards By Effect")]
    public void DebugDrawSpecificEffectCard()
    {
        if (!isDebugMode || handManager == null)
            return;

        List<Card> matchingCards = new List<Card>();
        List<CardData> allCardData = CardManager.GetAllCards();
        foreach (CardData cardData in allCardData)
        {
            if (cardData == null)
                continue;

            Card card = cardData.ToCard();
            if (card == null)
                continue;

            if (CardHasDebugTargetEffect(card))
            {
                matchingCards.Add(card);
            }
        }

        if (matchingCards.Count > 0)
        {
            Debug.Log($"[Debug] Added {matchingCards.Count} cards with {debugTargetEffect} effect from CardManager.");
            handManager.AddCard(matchingCards);
            if (battleContext != null)
            {
                battleContext.OnCardsDrawn(matchingCards.Count);
            }
        }
        else
        {
            Debug.LogWarning($"[Debug] No cards with {debugTargetEffect} effect found in deck.");
        }

        RefreshHandPlayableState();
        UpdateAllUI();
    }

    private bool CardHasDebugTargetEffect(Card card)
    {
        if (card == null || card.effects == null)
            return false;

        foreach (ICardEffect effect in card.effects)
        {
            if (EffectMatchesDebugTarget(effect))
                return true;
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
            case EffectType.Attack:
                if (effect is AttackEffect) return true;
                break;
            case EffectType.Execute:
                if (effect is ExecuteDamageEffect) return true;
                break;
            case EffectType.Heal:
                if (effect is HealEffect) return true;
                break;
            case EffectType.Buff:
                if (effect is BuffEffect) return true;
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
        if (effect is BarrierEffect barrier && barrier.onActions != null)
        {
            foreach (ICardEffect nested in barrier.onActions)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is ConsumeDefenseEffect consume && consume.nestedEffects != null)
        {
            foreach (ICardEffect nested in consume.nestedEffects)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
        }
        if (effect is RepeatEffect repeat && repeat.effectsToRepeat != null)
        {
            foreach (ICardEffect nested in repeat.effectsToRepeat)
            {
                if (EffectMatchesDebugTarget(nested))
                    return true;
            }
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

    public void ApplyCombatStartEffects()
    {
        // Placeholder: start-of-combat buffs/debuffs can be resolved here.
    }

    public void ApplyPlayerTurnStartEffects()
    {
        ApplyDebugEnergy();
        // Placeholder: player turn-start trigger effects.
    }

    public void ApplyPlayerTurnEndEffects()
    {
        if (playerData != null)
        {
            playerData.OnTurnEnd();
        }

        // Placeholder: player turn-end trigger effects.
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

    public void SetState(BattleTurnState newState)
    {
        CurrentTurnState = newState;
    }

    public void UpdateAllUI()
    {
        if (battleUI != null)
            battleUI.UpdateAllUI();
    }

    private void ApplyDebugEnergy()
    {
        if (!isDebugMode || playerData == null)
            return;

        playerData.maxEnergy = debugEnergyAmount;
        playerData.energy = debugEnergyAmount;
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
