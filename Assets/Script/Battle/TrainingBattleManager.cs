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
    [SerializeField] private DebugCardFilterType debugCardFilterType = DebugCardFilterType.Effect;
    public EffectType debugTargetEffect = EffectType.Barrier;
    [SerializeField] private DebugCardKeyword debugTargetKeyword = DebugCardKeyword.Keep;
    [SerializeField] private int debugTargetBuffId = 3002;
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

        if (!CanDrawCards())
        {
            return new List<Card>();
        }

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

        if (!CanDrawCards()) {
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

        if (!CanDrawCards())
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

    public void ResolveAdditionalTurnEndTriggers()
    {
        powerBuffRuntime?.ReplayTurnEndTriggeredEffects();
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
            card => playerData != null && card != null && CanPlayCard(card) && playerData.energy >= GetEffectiveCardCost(card),
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

    public bool CanDrawCards()
    {
        return powerBuffRuntime == null || powerBuffRuntime.CanDrawCards();
    }

    public bool CanGainCardsToHand()
    {
        return powerBuffRuntime == null || powerBuffRuntime.CanGainCardsToHand();
    }

    public int GetEffectiveCardCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        return powerBuffRuntime != null ? powerBuffRuntime.GetEffectiveCardCost(card) : card.cost;
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return powerBuffRuntime != null ? powerBuffRuntime.GetCardUseAllEnemiesDamage() : 0;
    }

    public void ApplyCardUseAllEnemiesDamage(int damage)
    {
        powerBuffRuntime?.ApplyCardUseAllEnemiesDamage(damage);
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

    public void ResolveDeferredTurnStartPowerEffects()
    {
        powerBuffRuntime?.ResolveDeferredTurnStartEffects();
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

    public void HandleMonsterHpLost(Monster monster, int hpLoss)
    {
        powerBuffRuntime?.OnMonsterHpLost(monster, hpLoss);
    }

    public void HandleMonsterDeath(Monster monster)
    {
        powerBuffRuntime?.OnMonsterDeath(monster);
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

        if (buffId == BattleRuntimeDefinitions.CorrosionBuffId
            && playerData != null
            && playerData.GetBuffStack(BattleRuntimeDefinitions.CorrosionEnhanceBuffId) > 0)
        {
            buffId = BattleRuntimeDefinitions.EnhancedCorrosionBuffId;
        }

        int crueltyStackBeforeApply = monster.GetBuffStack(BattleRuntimeDefinitions.CrueltyDebuffId);
        monster.AddBuff(buffId, amount);
        if (!BuffData.IsBeneficialBuffId(buffId))
        {
            powerBuffRuntime?.OnEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
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

