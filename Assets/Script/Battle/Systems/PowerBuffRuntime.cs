using System.Collections.Generic;
using UnityEngine;

public class PowerBuffRuntime
{
    private const int PotionEnhanceBuffId = 1002;
    private const int PotionCycleBuffId = 1003;
    private const int GlacierShapeEnhanceBuffId = 1004;
    private const int UnplayableUnlockBuffId = 1006;
    private const int DoubleJackBuffId = 1007;
    private const int DuplicateGenerateBuffId = 1008;
    private const int HighCostRepeatBuffId = 1009;
    private const int StraightBuffId = 1010;
    private const int RuneBarrierBuffId = 1011;
    private const int PermanentBarrierRetentionBuffId = 1013;

    private const int DamageAmplifyBuffId = 3002;
    private const int PotionFactoryBuffId = 3007;
    private const int ExtraDrawBuffId = 3008;
    private const int ThornBuffId = 3010;
    private const int GlacierBondBuffId = 3012;
    private const int AbsoluteZeroBuffId = 3013;
    private const int GlacierShapeOnHitBuffId = 3014;
    private const int ColdAirBuffId = 3015;
    private const int OverheatBuffId = 3017;
    private const int OverheatGrowthBuffId = 3018;
    private const int FlameConductionBuffId = 3019;
    private const int LavaSkinBuffId = 3020;
    private const int CounterattackBuffId = 3021;
    private const int IntimidationBuffId = 3022;
    private const int RetainChoiceBuffId = 3028;
    private const int JokerPowerBuffId = 3029;
    private const int RuneGenerationBuffId = 3032;

    private const int BurnBuffId = 4003;
    private const int FreezeBuffId = 4004;
    private const int CorrosionBuffId = 4001;

    private static readonly int[] BasePotionCardIds = { 101080, 101082, 101084, 101086 };
    private static readonly int[] JokerUpgradeCardIds = { 102041, 102042, 102043, 102044, 102045 };

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

    public PowerBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public void ResetForCombat()
    {
        playedCardCounterForDoubleJack = 0;
        remainingHighCostRepeatCount = 0;
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
            AddGeneratedCardsToHand(CreateRandomCards(BasePotionCardIds, potionFactory));
        }

        int jokerPower = GetPlayerBuffStack(JokerPowerBuffId);
        if (jokerPower > 0)
        {
            AddGeneratedCardsToHand(CreateRandomCards(JokerUpgradeCardIds, jokerPower));
        }

        int straightStack = GetPlayerBuffStack(StraightBuffId);
        if (straightStack > 0)
        {
            ApplyStraightEffect(straightStack);
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
        if (isRepeatedEffect || playedCard == null || playedCard.cost < 2 || remainingHighCostRepeatCount <= 0)
        {
            return 0;
        }

        remainingHighCostRepeatCount--;
        return 1;
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

        if (BuffData.IsBeneficialBuffId(buffId))
        {
            return;
        }

        int cruelty = GetPlayerBuffStack(3005);
        if (cruelty > 0)
        {
            targetMonster.TakeDamage(cruelty, 0);
        }

        int brutality = GetPlayerBuffStack(3006);
        if (brutality > 0)
        {
            List<Monster> targets = battleManager.GetLivingMonsters();
            foreach (Monster monster in targets)
            {
                monster.TakeDamage(brutality, 0);
            }
        }
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

    private void ApplyStraightEffect(int stack)
    {
        int drawCount = stack * 2;
        List<Card> drawnCards = battleManager.DrawCardsAndGet(drawCount);
        if (drawnCards.Count <= 0 || battleManager.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        int discardCount = Mathf.Min(stack, drawnCards.Count);
        for (int i = 0; i < discardCount; i++)
        {
            Card cardToDiscard = drawnCards[drawnCards.Count - 1 - i];
            if (cardToDiscard == null || !battleManager.handManager.RemoveCard(cardToDiscard))
            {
                continue;
            }

            battleManager.usableDeckManager.AddToDiscard(cardToDiscard);
            battleManager.battleContext?.OnCardsDiscarded(1);
        }
    }

    private List<Card> CreateRandomCards(int[] pool, int count)
    {
        List<Card> generatedCards = new();
        if (pool == null || pool.Length == 0 || count <= 0)
        {
            return generatedCards;
        }

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Length);
            Card generatedCard = CardManager.GetCardAsCard(pool[index]);
            if (generatedCard != null)
            {
                generatedCards.Add(generatedCard);
            }
        }

        return generatedCards;
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
