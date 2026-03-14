using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public class PowerBuffRuntime
{
    private const string BasePotionGroupName = "isla_potion_even_base_pool";
    private const string JokerUpgradeGroupName = "enforce_102040";

    private static readonly Dictionary<int, int> GeneratedUpgradeMap = new()
    {
        { 101080, 101081 },
        { 101082, 101083 },
        { 101084, 101085 },
        { 101086, 101087 },
        { 301080, 301081 }
    };

    private readonly TrainingBattleManager battleManager;

    private int playedCardCounterForDoubleJack;
    private int remainingHighCostRepeatCount;
    private int pendingStraightDiscardCount;

    public PowerBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void ResetForCombat()
    {
        playedCardCounterForDoubleJack = 0;
        remainingHighCostRepeatCount = 0;
        pendingStraightDiscardCount = 0;
    }

    public bool CanPlayCard(Card card)
    {
        if (card == null)
        {
            return false;
        }

        if (!card.HasKeyword(CardKeywordIds.Unplayable))
        {
            return true;
        }

        return GetPlayerBuffStack(UnplayableUnlockBuffId) > 0;
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        return card != null
            && card.HasKeyword(CardKeywordIds.Unplayable)
            && GetPlayerBuffStack(UnplayableUnlockBuffId) > 0;
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        return IsPotionCard(card) && GetPlayerBuffStack(PotionCycleBuffId) > 0;
    }

    public bool HasPermanentBarrierRetention()
    {
        return GetPlayerBuffStack(PermanentBarrierRetentionBuffId) > 0;
    }

    public bool CanDrawCards()
    {
        return GetPlayerBuffStack(DrawLockBuffId) <= 0;
    }

    public bool CanGainCardsToHand()
    {
        return GetPlayerBuffStack(DrawLockBuffId) <= 0;
    }

    public int GetEffectiveCardCost(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        return GetPlayerBuffStack(NextCardFreeBuffId) > 0 ? 0 : card.cost;
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return GetPlayerBuffStack(CardUseAllEnemiesDamageBuffId) + GetPlayerBuffStack(BlessingPulseBuffId);
    }

    public int GetAdditionalBarrierGain()
    {
        return GetPlayerBuffStack(GlacierBondBuffId);
    }

    public int GetTurnEndRetainCount()
    {
        return GetPlayerBuffStack(RetainChoiceBuffId);
    }

    public void OnTurnStart()
    {
        remainingHighCostRepeatCount = GetPlayerBuffStack(HighCostRepeatBuffId);

        int extraDraw = GetPlayerBuffStack(ExtraDrawBuffId);
        if (extraDraw > 0)
        {
            battleManager.AddTurnStartDrawModifier(extraDraw);
        }

        int overheatGrowth = GetPlayerBuffStack(OverheatGrowthBuffId);
        if (overheatGrowth > 0)
        {
            battleManager.ApplyBuffToPlayer(OverheatBuffId, overheatGrowth);
        }

        int potionFactory = GetPlayerBuffStack(PotionFactoryBuffId);
        if (potionFactory > 0)
        {
            AddGeneratedCardsToHand(CreateRandomCardsFromGroup(BasePotionGroupName, potionFactory));
        }

        int delayedPotionFactory = GetPlayerBuffStack(DelayedPotionFactoryBuffId);
        if (delayedPotionFactory > 0)
        {
            AddGeneratedCardsToHand(CreateRandomCardsFromGroup(BasePotionGroupName, delayedPotionFactory));
            battleManager.playerData?.ConsumeBuffStack(DelayedPotionFactoryBuffId, delayedPotionFactory);
        }

        int jokerPower = GetPlayerBuffStack(JokerPowerBuffId);
        if (jokerPower > 0)
        {
            AddGeneratedCardsToHand(CreateRandomUniqueUpgradeCards(jokerPower));
        }

        int straightStack = GetPlayerBuffStack(StraightBuffId);
        if (straightStack > 0)
        {
            battleManager.AddTurnStartDrawModifier(straightStack * 2);
            pendingStraightDiscardCount += straightStack;
        }

        int absoluteZero = GetPlayerBuffStack(AbsoluteZeroBuffId);
        if (absoluteZero > 0)
        {
            battleManager.ApplyBuffToAllEnemies(FreezeBuffId, absoluteZero);
        }
    }

    public void OnTurnEnd()
    {
        int runeGeneration = GetPlayerBuffStack(RuneGenerationBuffId);
        if (runeGeneration > 0)
        {
            battleManager.ApplyBuffToPlayer(3031, runeGeneration);
        }

        int runeBarrier = GetPlayerBuffStack(RuneBarrierBuffId);
        if (runeBarrier > 0 && battleManager.playerData != null)
        {
            int runeStack = battleManager.playerData.GetBuffStack(3031);
            if (runeStack > 0)
            {
                battleManager.playerData.AddDefense(runeStack * runeBarrier);
            }
        }

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            if (monster == null)
            {
                continue;
            }

            monster.ConsumeBuffStack(LifeLinkBuffId, 1);
        }
    }

    public void ResolveDeferredTurnStartEffects()
    {
        ResolveStraightDiscardSelection();
    }

    public void OnCardPlayed(Card playedCard, Monster originalTarget, bool isRepeatedEffect)
    {
        if (playedCard == null)
        {
            return;
        }

        if (!isRepeatedEffect)
        {
            ApplyDoubleJack(playedCard);
            ApplyPotionUseEffects(playedCard);
        }
    }

    public int ConsumeRepeatCount(Card playedCard, bool isRepeatedEffect)
    {
        if (isRepeatedEffect || playedCard == null)
        {
            return 0;
        }

        int repeatCount = 0;

        if (playedCard.cost >= 2 && remainingHighCostRepeatCount > 0)
        {
            remainingHighCostRepeatCount--;
            repeatCount++;
        }

        int repeatNextCard = GetPlayerBuffStack(RepeatNextCardBuffId);
        if (repeatNextCard > 0)
        {
            battleManager.playerData?.ConsumeBuffStack(RepeatNextCardBuffId, repeatNextCard);
            repeatCount += repeatNextCard;
        }

        return repeatCount;
    }

    public void OnAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        if (targetMonster == null || targetMonster.IsDead())
        {
            return;
        }

        int flameConduction = GetPlayerBuffStack(FlameConductionBuffId);
        if (flameConduction > 0)
        {
            battleManager.ApplyBuffToMonster(targetMonster, BurnBuffId, flameConduction);
        }

        int intimidation = GetPlayerBuffStack(IntimidationBuffId);
        if (intimidation > 0 && barrierBefore > 0 && barrierAfter <= 0)
        {
            battleManager.ApplyBuffToPlayer(DamageAmplifyBuffId, intimidation);
        }
    }

    public void OnPlayerHit(Monster attacker, int blockedDamage, int hpDamage)
    {
        if (attacker == null || attacker.IsDead())
        {
            return;
        }

        int thornDamage = GetPlayerBuffStack(ThornBuffId);
        if (thornDamage > 0)
        {
            attacker.TakeDamage(thornDamage, 0);
        }

        int lavaSkin = GetPlayerBuffStack(LavaSkinBuffId);
        if (lavaSkin > 0)
        {
            battleManager.ApplyBuffToMonster(attacker, BurnBuffId, lavaSkin);
        }

        int counterattack = GetPlayerBuffStack(CounterattackBuffId);
        if (counterattack > 0)
        {
            attacker.TakeDamage(counterattack, 0);
        }

        int glacierShapeOnHit = GetPlayerBuffStack(GlacierShapeOnHitBuffId);
        if (glacierShapeOnHit > 0)
        {
            List<Card> generatedCards = new();
            for (int i = 0; i < glacierShapeOnHit; i++)
            {
                Card card = CardManager.GetCardAsCard(301080);
                if (card != null)
                {
                    generatedCards.Add(card);
                }
            }

            AddGeneratedCardsToHand(generatedCards);
        }
    }

    public void OnPlayerBarrierReduced(int reducedAmount)
    {
        if (reducedAmount <= 0)
        {
            return;
        }

        int coldAir = GetPlayerBuffStack(ColdAirBuffId);
        if (coldAir > 0)
        {
            battleManager.ApplyBuffToAllEnemies(FreezeBuffId, coldAir);
        }
    }

    public void OnEnemyDebuffApplied(Monster targetMonster, int buffId, int amount)
    {
        if (targetMonster == null || targetMonster.IsDead() || amount <= 0)
        {
            return;
        }

        // 버프 ID 앞자리 홀짝 규칙으로 해로운 효과 여부를 판정
        if (BuffData.IsBeneficialBuffId(buffId))
        {
            return;
        }

        int cruelty = targetMonster.GetBuffStack(CrueltyDebuffId);
        if (cruelty > 0)
        {
            targetMonster.TakeDamage(cruelty, 0);
        }

        int brutality = GetPlayerBuffStack(BioExperimentAllBuffId);
        if (brutality > 0)
        {
            List<Monster> targets = battleManager.GetLivingMonsters();
            foreach (Monster monster in targets)
            {
                monster.TakeDamage(brutality, 0);
            }
        }
    }

    public void OnMonsterHpLost(Monster targetMonster, int hpLoss)
    {
        if (targetMonster == null || hpLoss <= 0 || battleManager.playerData == null)
        {
            return;
        }

        int lifeLink = targetMonster.GetBuffStack(LifeLinkBuffId);
        if (lifeLink > 0)
        {
            battleManager.playerData.Heal(hpLoss * lifeLink);
        }
    }

    public void OnMonsterDeath(Monster deadMonster)
    {
        if (deadMonster == null || deadMonster.GetBuffStack(FlameTransferBuffId) <= 0)
        {
            return;
        }

        int burnStack = deadMonster.GetBuffStack(BurnBuffId);
        if (burnStack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToAllEnemies(BurnBuffId, burnStack);
    }

    public void ApplyCardUseAllEnemiesDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        List<Monster> targets = battleManager.GetLivingMonsters();
        foreach (Monster monster in targets)
        {
            monster.TakeDamage(damage, 0);
        }

        battleManager.battleContext?.OnDamageDealt(damage * targets.Count);
    }

    public List<Card> ProcessGeneratedCards(List<Card> generatedCards, bool allowDuplicateGeneration)
    {
        List<Card> processedCards = new();
        if (generatedCards == null)
        {
            return processedCards;
        }

        foreach (Card generatedCard in generatedCards)
        {
            Card processedCard = ApplyGeneratedCardTransform(generatedCard);
            if (processedCard != null)
            {
                processedCards.Add(processedCard);
            }
        }

        if (!allowDuplicateGeneration)
        {
            return processedCards;
        }

        int duplicateStack = GetPlayerBuffStack(DuplicateGenerateBuffId);
        if (duplicateStack <= 0 || processedCards.Count == 0)
        {
            return processedCards;
        }

        List<Card> duplicatedCards = new();
        foreach (Card processedCard in processedCards)
        {
            if (processedCard == null)
            {
                continue;
            }

            for (int i = 0; i < duplicateStack; i++)
            {
                Card duplicatedCard = processedCard.CloneForRuntimeCopy();
                if (duplicatedCard != null)
                {
                    duplicatedCards.Add(duplicatedCard);
                }
            }
        }

        if (duplicatedCards.Count > 0)
        {
            processedCards.AddRange(ProcessGeneratedCards(duplicatedCards, false));
        }

        return processedCards;
    }

    public int ResolvePersistentUpgradeCardId(int cardId)
    {
        int resolvedCardId = cardId;
        if (GetPlayerBuffStack(PotionEnhanceBuffId) > 0
            && GeneratedUpgradeMap.TryGetValue(resolvedCardId, out int upgradedPotionId)
            && IsPotionCardId(resolvedCardId))
        {
            resolvedCardId = upgradedPotionId;
        }

        if (GetPlayerBuffStack(GlacierShapeEnhanceBuffId) > 0
            && resolvedCardId == 301080
            && GeneratedUpgradeMap.TryGetValue(resolvedCardId, out int upgradedGlacierShapeId))
        {
            resolvedCardId = upgradedGlacierShapeId;
        }

        return resolvedCardId;
    }

    private void ApplyDoubleJack(Card playedCard)
    {
        int doubleJack = GetPlayerBuffStack(DoubleJackBuffId);
        if (doubleJack <= 0)
        {
            return;
        }

        playedCardCounterForDoubleJack++;
        while (playedCardCounterForDoubleJack >= 2)
        {
            playedCardCounterForDoubleJack -= 2;
            battleManager.playerData?.Heal(doubleJack);
        }
    }

    private void ApplyPotionUseEffects(Card playedCard)
    {
        if (!IsPotionCard(playedCard))
        {
            return;
        }

        int corrosionOnPotionUse = GetPlayerBuffStack(3004);
        if (corrosionOnPotionUse > 0)
        {
            Monster randomTarget = PickRandomLivingMonster();
            if (randomTarget != null)
            {
                battleManager.ApplyBuffToMonster(randomTarget, CorrosionBuffId, corrosionOnPotionUse);
            }
        }

        int potionCycle = GetPlayerBuffStack(PotionCycleBuffId);
        if (potionCycle > 0)
        {
            battleManager.DrawCards(potionCycle);
        }
    }

    private void ResolveStraightDiscardSelection()
    {
        if (pendingStraightDiscardCount <= 0 || battleManager.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        List<Card> selectableCards = battleManager.handManager.GetHandCards();
        int discardCount = Mathf.Min(pendingStraightDiscardCount, selectableCards.Count);
        pendingStraightDiscardCount = 0;
        if (discardCount <= 0)
        {
            return;
        }

        bool opened = battleManager.OpenSelectCardPanel(selectableCards, discardCount, DiscardStraightCards);
        if (opened)
        {
            return;
        }

        List<Card> fallbackSelection = new();
        for (int i = 0; i < discardCount; i++)
        {
            Card card = selectableCards[i];
            if (card != null)
            {
                fallbackSelection.Add(card);
            }
        }

        DiscardStraightCards(fallbackSelection);
    }

    private void DiscardStraightCards(List<Card> selectedCards)
    {
        if (battleManager.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        int discardedCount = 0;
        List<Card> safeSelectedCards = selectedCards ?? new List<Card>();
        foreach (Card card in safeSelectedCards)
        {
            if (card == null || !battleManager.handManager.RemoveCard(card))
            {
                continue;
            }

            battleManager.usableDeckManager.AddToDiscard(card);
            discardedCount++;
        }

        if (discardedCount > 0)
        {
            battleManager.battleContext?.OnCardsDiscarded(discardedCount);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private List<Card> CreateRandomCardsFromGroup(string groupName, int count)
    {
        List<Card> generatedCards = new();
        if (count <= 0)
        {
            return generatedCards;
        }

        List<int> pool = CardManager.GetCardIdsByGroup(groupName);
        if (pool == null || pool.Count == 0)
        {
            return generatedCards;
        }

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            Card generatedCard = CardManager.GetCardAsCard(pool[index]);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        return generatedCards;
    }

    private List<Card> CreateRandomUniqueUpgradeCards(int count)
    {
        List<int> pool = GetUniqueUpgradeCardPool();
        if (pool.Count == 0)
        {
            return CreateRandomCardsFromGroup(JokerUpgradeGroupName, count);
        }

        List<Card> generatedCards = new();
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);
            Card generatedCard = CardManager.GetCardAsCard(pool[index]);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        return generatedCards;
    }

    private List<int> GetUniqueUpgradeCardPool()
    {
        List<int> pool = new();
        HashSet<Character> selectedCharacters = null;
        if (SelectedButtonControl.selectedCharacterList != null && SelectedButtonControl.selectedCharacterList.Count > 0)
        {
            selectedCharacters = new HashSet<Character>(SelectedButtonControl.selectedCharacterList);
        }

        List<CardData> allCards = CardManager.GetAllCards();
        foreach (CardData cardData in allCards)
        {
            if (!IsUniqueUpgradeCard(cardData, selectedCharacters))
            {
                continue;
            }

            if (!pool.Contains(cardData.cardId))
            {
                pool.Add(cardData.cardId);
            }
        }

        return pool;
    }

    private static bool IsUniqueUpgradeCard(CardData cardData, HashSet<Character> selectedCharacters)
    {
        if (cardData == null)
        {
            return false;
        }

        if (selectedCharacters != null && !selectedCharacters.Contains(cardData.character))
        {
            return false;
        }

        if (Mathf.Abs(cardData.cardId) % 10 == 0)
        {
            return false;
        }

        return cardData.keywords != null && cardData.keywords.Contains(CardKeywordIds.Unique);
    }

    private void AddGeneratedCardsToHand(List<Card> generatedCards)
    {
        List<Card> processedCards = ProcessGeneratedCards(generatedCards, true);
        if (processedCards.Count == 0 || battleManager.handManager == null)
        {
            return;
        }

        battleManager.handManager.AddCard(processedCards);
        battleManager.UpdateAllUI();
    }

    private Card ApplyGeneratedCardTransform(Card generatedCard)
    {
        if (generatedCard == null)
        {
            return null;
        }

        int transformedCardId = ResolvePersistentUpgradeCardId(generatedCard.cardId);

        if (transformedCardId == generatedCard.cardId)
        {
            return generatedCard;
        }

        Card transformedCard = CardManager.GetCardAsCard(transformedCardId);
        return transformedCard ?? generatedCard;
    }

    private Monster PickRandomLivingMonster()
    {
        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters == null || livingMonsters.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, livingMonsters.Count);
        return livingMonsters[index];
    }

    private int GetPlayerBuffStack(int buffId)
    {
        return battleManager?.playerData != null ? battleManager.playerData.GetBuffStack(buffId) : 0;
    }

    private static bool IsPotionCard(Card card)
    {
        return card != null && IsPotionCardId(card.cardId);
    }

    private static bool IsPotionCardId(int cardId)
    {
        return cardId >= 101080 && cardId <= 101087;
    }
}
