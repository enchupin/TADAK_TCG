using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public class BattleBuffController
{
    private readonly TrainingBattleManager battleManager;
    private readonly PlayerBuffRuntimeService playerBuffRuntimeService;
    private readonly MonsterBuffRuntimeService monsterBuffRuntimeService;
    private readonly FeatherBuffScript featherBuffScript;
    private readonly HashSet<Monster> monsterHpLossHealPlayerTargetsThisTurn = new();

    public BattleBuffController(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
        playerBuffRuntimeService = new PlayerBuffRuntimeService(battleManager);
        monsterBuffRuntimeService = new MonsterBuffRuntimeService(battleManager);
        featherBuffScript = new FeatherBuffScript(battleManager, playerBuffRuntimeService);
    }

    public void ResetForCombat()
    {
        monsterHpLossHealPlayerTargetsThisTurn.Clear();
        playerBuffRuntimeService.ResetForCombat();
        featherBuffScript.ResetForCombat();
    }

    public void ApplyPlayerTurnStartEffects()
    {
        monsterHpLossHealPlayerTargetsThisTurn.Clear();
        playerBuffRuntimeService.OnPlayerTurnStart();
        featherBuffScript.OnTurnStart();
    }

    public void ApplyPlayerTurnEndEffects()
    {
        playerBuffRuntimeService.OnPlayerTurnEnd();
        playerBuffRuntimeService.ReplayTurnEndTriggeredEffects();
        monsterBuffRuntimeService.OnPlayerTurnEnd();
        monsterHpLossHealPlayerTargetsThisTurn.Clear();
    }

    public void ReplayAdditionalTurnEndTriggers()
    {
        playerBuffRuntimeService.ReplayTurnEndTriggeredEffects();
    }

    public bool CanPlayCard(Card card)
    {
        return playerBuffRuntimeService.CanPlayCard(card);
    }

    public bool ShouldPotionGoToDiscardInsteadOfExhaust(Card card)
    {
        return playerBuffRuntimeService.ShouldPotionGoToDiscardInsteadOfExhaust(card);
    }

    public bool ShouldExhaustUnlockedUnplayableCard(Card card)
    {
        return playerBuffRuntimeService.ShouldExhaustUnlockedUnplayableCard(card);
    }

    public bool CanDrawCards()
    {
        return playerBuffRuntimeService.CanDrawCards();
    }

    public bool CanGainCardsToHand()
    {
        return playerBuffRuntimeService.CanGainCardsToHand();
    }

    public bool CanGainCardsToHandFrom(MoveZoneType from, string subject = null)
    {
        return playerBuffRuntimeService.CanGainCardsToHandFrom(from, subject);
    }

    public int GetEffectiveCardCost(Card card)
    {
        return playerBuffRuntimeService.GetEffectiveCardCost(card);
    }

    public int GetCardUseAllEnemiesDamage()
    {
        return playerBuffRuntimeService.GetCardUseAllEnemiesDamage();
    }

    public void ApplyCardUseAllEnemiesDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        int resolvedDamage = battleManager.ResolvePlayerEffectDamage(damage);
        if (resolvedDamage <= 0)
        {
            return;
        }

        List<Monster> targets = battleManager.GetLivingMonsters();
        int totalDamageDealt = 0;
        foreach (Monster monster in targets)
        {
            int dealtDamage = monster.TakeDamage(resolvedDamage, 0);
            totalDamageDealt += dealtDamage;
            HandlePlayerDamageDealt(monster, dealtDamage);
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
    }

    public int GetAdditionalBarrierGain()
    {
        return playerBuffRuntimeService.GetAdditionalBarrierGain();
    }

    public int ResolvePlayerBarrierGain(int amount)
    {
        int baseAmount = Mathf.Max(0, amount) + GetAdditionalBarrierGain();
        return playerBuffRuntimeService.ResolveBarrierGain(baseAmount);
    }

    public bool ShouldRetainPlayerBarrierOnTurnStart()
    {
        return HasPermanentBarrierRetention() || playerBuffRuntimeService.TryConsumeBarrierRetentionOnTurnStart();
    }

    public int GetPlayerTurnStartBarrierLoss(int currentDefense)
    {
        return playerBuffRuntimeService.GetTurnStartBarrierLoss(currentDefense);
    }

    public int GetTurnEndRetainCount()
    {
        return playerBuffRuntimeService.GetTurnEndRetainCount();
    }

    public int GetCardBaseDamageBonus(Card sourceCard, bool isAttackEffect)
    {
        return playerBuffRuntimeService.GetCardBaseDamageBonus(sourceCard, isAttackEffect);
    }

    public int ApplyCardDamageRuntimeModifiers(Card sourceCard, int damage)
    {
        return playerBuffRuntimeService.ModifyCardDamage(sourceCard, damage);
    }

    public int GetPlayerCalculatedCardDamageBonus(float strengthMultiplier)
    {
        return playerBuffRuntimeService.GetCalculatedCardDamageBonus(strengthMultiplier);
    }

    public float GetPlayerCalculatedCardBaseMultiplier(float baseMultiplier)
    {
        return playerBuffRuntimeService.GetCalculatedCardBaseMultiplier(baseMultiplier);
    }

    public float GetPlayerOutgoingDamageMultiplier()
    {
        return playerBuffRuntimeService.GetOutgoingDamageMultiplier();
    }

    public int ResolvePlayerIncomingDamage(int damage, Monster attacker)
    {
        int safeDamage = Mathf.Max(0, damage);
        float multiplier = playerBuffRuntimeService.GetIncomingDamageMultiplier(attacker);
        int finalDamage = Mathf.FloorToInt(safeDamage * multiplier);
        return playerBuffRuntimeService.ClampIncomingDamage(attacker, finalDamage);
    }

    public bool TryPreventPlayerIncomingDamage(int damage, Monster attacker)
    {
        return playerBuffRuntimeService.TryPreventIncomingDamage(attacker, damage);
    }

    public void ConsumePlayerIncomingDamageBuffs(Monster attacker, int damage)
    {
        playerBuffRuntimeService.ConsumeIncomingDamageBuff(attacker, damage);
    }

    public int ResolvePersistentUpgradeCardId(int cardId, int sourceBuffId = 0)
    {
        int resolvedCardId = playerBuffRuntimeService.ResolvePersistentUpgradeCardId(cardId, sourceBuffId);
        return monsterBuffRuntimeService.ResolvePersistentUpgradeCardId(resolvedCardId, sourceBuffId);
    }

    public bool HasPermanentBarrierRetention()
    {
        return playerBuffRuntimeService.HasPermanentBarrierRetention();
    }

    public void HandlePlayedCardEffects(Card playedCard, Monster originalTarget, bool isRepeatedEffect)
    {
        playerBuffRuntimeService.OnCardPlayed(playedCard, originalTarget, isRepeatedEffect);
    }

    public void ResolveDeferredTurnStartEffects()
    {
        playerBuffRuntimeService.ResolveDeferredTurnStartEffects();
    }

    public int ConsumeRepeatedPlayCount(Card playedCard, bool isRepeatedEffect)
    {
        return playerBuffRuntimeService.ConsumeRepeatCount(playedCard, isRepeatedEffect);
    }

    public void RegisterMonsterHpLossHealPlayerThisTurn(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return;
        }

        monsterHpLossHealPlayerTargetsThisTurn.Add(monster);
    }

    public void HandlePlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        playerBuffRuntimeService.OnPlayerAttackResolved(targetMonster, barrierBefore, barrierAfter);
    }

    public void HandlePlayerHit(Monster attacker, int blockedDamage, int hpDamage)
    {
        playerBuffRuntimeService.OnPlayerHit(attacker, blockedDamage, hpDamage);
    }

    public void HandlePlayerBarrierReduced(int reducedAmount)
    {
        playerBuffRuntimeService.OnPlayerBarrierReduced(reducedAmount);
    }

    public void HandleCardsExhausted(int count)
    {
        playerBuffRuntimeService.OnCardsExhausted(count);
    }

    public int TriggerFeather(TargetType target, int repeatCount = 1)
    {
        return featherBuffScript.Trigger(target, repeatCount);
    }

    public int TriggerFeatherUntilEmpty(TargetType target)
    {
        return featherBuffScript.TriggerUntilEmpty(target);
    }

    public int ReplayExhaustedFeathers()
    {
        return featherBuffScript.ReplayExhaustedFeathers();
    }

    public bool TryConsumeSoulProtection()
    {
        return playerBuffRuntimeService.TryConsumeFatalDamage();
    }

    public bool TryApplyToPlayer(int buffId, int amount)
    {
        if (featherBuffScript.TryApplyToPlayer(buffId, amount))
        {
            HandleBuffApplied(buffId);
            return true;
        }

        if (playerBuffRuntimeService.TryApplyToPlayer(buffId, amount))
        {
            HandleBuffApplied(buffId);
            return true;
        }

        return false;
    }

    public bool TryApplyToMonster(int buffId, Monster monster, int amount)
    {
        if (featherBuffScript.TryApplyToMonster(buffId, monster, amount))
        {
            HandleBuffApplied(buffId);
            return true;
        }

        return false;
    }

    public bool TryApplyToAllEnemies(int buffId, int amount)
    {
        if (featherBuffScript.TryApplyToAllEnemies(buffId, amount))
        {
            HandleBuffApplied(buffId);
            return true;
        }

        return false;
    }

    public int ResolveAppliedMonsterBuffId(Monster targetMonster, int buffId, int amount)
    {
        return playerBuffRuntimeService.ResolveAppliedMonsterBuffId(targetMonster, buffId, amount);
    }

    public void HandleEnemyDebuffApplied(Monster monster, int buffId, int amount, int crueltyStackBeforeApply = -1)
    {
        if (BuffData.IsBeneficialBuffId(buffId))
        {
            return;
        }

        monsterBuffRuntimeService.OnEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
        playerBuffRuntimeService.OnEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
    }

    public void HandleMonsterHpLost(Monster monster, int hpLoss)
    {
        if (monster != null
            && hpLoss > 0
            && battleManager.playerData != null
            && monsterHpLossHealPlayerTargetsThisTurn.Contains(monster))
        {
            battleManager.playerData.Heal(hpLoss);
        }

        monsterBuffRuntimeService.OnMonsterHpLost(monster, hpLoss);
    }

    public void HandlePlayerHpLost(int hpLoss)
    {
        playerBuffRuntimeService.OnPlayerHpLost(hpLoss);
        monsterBuffRuntimeService.OnPlayerHpLost(hpLoss);
    }

    public void HandlePlayerDamageDealt(Monster monster, int dealtDamage)
    {
        playerBuffRuntimeService.OnPlayerDamageDealt(monster, dealtDamage);
    }

    public void HandleBattleEnded(bool isVictory)
    {
        playerBuffRuntimeService.OnBattleEnded(isVictory);
    }

    public void HandleMonsterBuffApplied(Monster monster, int buffId, int amount)
    {
        monsterBuffRuntimeService.OnBuffApplied(monster, buffId, amount);
    }

    public void ApplyMonsterTurnStartEffects(Monster monster)
    {
        monsterBuffRuntimeService.OnMonsterTurnStart(monster);
    }

    public void ApplyMonsterTurnEndEffects(Monster monster)
    {
        monsterBuffRuntimeService.OnMonsterTurnEnd(monster);
    }

    public bool ShouldKeepMonsterBarrierOnTurnStart(Monster monster)
    {
        return monsterBuffRuntimeService.ShouldKeepBarrierOnTurnStart(monster);
    }

    public int ResolveMonsterIncomingDamage(Monster monster, int damage)
    {
        int safeDamage = Mathf.Max(0, damage);
        float multiplier = monsterBuffRuntimeService.GetIncomingDamageMultiplier(monster);
        return Mathf.FloorToInt(safeDamage * multiplier);
    }

    public void ConsumeMonsterIncomingDamageBuffs(Monster monster, int damage)
    {
        if (monster == null || damage <= 0)
        {
            return;
        }

        monsterBuffRuntimeService.TryConsumeIncomingDamageBuff(monster);
    }

    public void HandleMonsterDefenseChanged(Monster monster, int previousDefense, int currentDefense)
    {
        monsterBuffRuntimeService.OnDefenseChanged(monster, previousDefense, currentDefense);
    }

    public int ResolveMonsterBarrierGain(Monster monster, int amount)
    {
        return monsterBuffRuntimeService.ResolveBarrierGain(monster, amount);
    }

    public int ModifyMonsterOutgoingDamage(Monster monster, int damage)
    {
        return monsterBuffRuntimeService.ModifyOutgoingDamage(monster, damage);
    }

    public void HandleMonsterAttackResolved(Monster monster, PlayerData target, int attemptedDamage, int hpDamage)
    {
        monsterBuffRuntimeService.OnMonsterAttackResolved(monster, target, attemptedDamage, hpDamage);
    }

    public void HandleMonsterBeforeTakeDamage(Monster monster, int incomingDamage)
    {
        monsterBuffRuntimeService.OnMonsterBeforeTakeDamage(monster, incomingDamage);
    }

    public void HandleMonsterAfterTakeDamage(Monster monster, int incomingDamage, int damageAfterDefense)
    {
        monsterBuffRuntimeService.OnMonsterAfterTakeDamage(monster, incomingDamage, damageAfterDefense);
    }

    public bool CanMonsterReceiveDamage(Monster monster, int incomingDamage)
    {
        return monsterBuffRuntimeService.CanReceiveDamage(monster, incomingDamage);
    }

    public bool CanMonsterRevive(Monster monster)
    {
        return monsterBuffRuntimeService.CanRevive(monster);
    }

    public void HandleMonsterDeath(Monster monster)
    {
        monsterHpLossHealPlayerTargetsThisTurn.Remove(monster);
        monsterBuffRuntimeService.OnMonsterDeath(monster);
    }

    public void HandleMonsterLeaveCombat(Monster monster)
    {
        monsterHpLossHealPlayerTargetsThisTurn.Remove(monster);
        monsterBuffRuntimeService.OnMonsterLeaveCombat(monster);
    }

    public void HandleMonsterRevived(Monster monster)
    {
        monsterBuffRuntimeService.OnMonsterRevived(monster);
    }

    public List<Card> ProcessGeneratedCards(List<Card> generatedCards, bool allowDuplicateGeneration = true)
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

        int duplicateCount = playerBuffRuntimeService.GetGeneratedCardDuplicateCount();
        if (duplicateCount <= 0 || processedCards.Count == 0)
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

            for (int i = 0; i < duplicateCount; i++)
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

    public Card ApplyPersistentUpgradeToCard(Card card, int sourceBuffId = 0)
    {
        if (card == null)
        {
            return null;
        }

        int upgradedCardId = ResolvePersistentUpgradeCardId(card.cardId, sourceBuffId);
        if (upgradedCardId == card.cardId)
        {
            return card;
        }

        Card upgradedTemplate = CardManager.GetCardAsCard(upgradedCardId);
        if (upgradedTemplate != null)
        {
            card.ApplyTemplate(upgradedTemplate);
        }

        return card;
    }

    public void HandleBuffApplied(int buffId)
    {
        if (!IsPersistentUpgradeBuff(buffId))
        {
            return;
        }

        List<Card> changedHandCards = new();
        bool hasChanges = false;

        hasChanges |= ApplyPersistentCardBuffChanges(battleManager.handManager?.GetHandCards(), changedHandCards, buffId);
        hasChanges |= ApplyPersistentCardBuffChanges(battleManager.usableDeckManager?.GetDrawPile(), null, buffId);
        hasChanges |= ApplyPersistentCardBuffChanges(battleManager.usableDeckManager?.GetDiscardPile(), null, buffId);
        hasChanges |= ApplyPersistentCardBuffChanges(battleManager.usableDeckManager?.GetExhaustPile(), null, buffId);

        if (!hasChanges)
        {
            return;
        }

        if (battleManager.handManager != null && changedHandCards.Count > 0)
        {
            battleManager.handManager.RefreshCardDisplays(changedHandCards);
            battleManager.RefreshHandPlayableState();
        }

        battleManager.UpdateAllUI();
    }

    private static bool IsPersistentUpgradeBuff(int buffId)
    {
        return buffId == PotionEnhanceBuffId
            || buffId == GlacierShapeEnhanceBuffId
            || buffId == FeatherEnhanceBuffId
            || buffId == PoisonUpgradeBuffId;
    }

    private bool ApplyPersistentCardBuffChanges(List<Card> cards, List<Card> changedCards, int sourceBuffId)
    {
        if (cards == null || cards.Count == 0)
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

            int originalCardId = card.cardId;
            ApplyPersistentUpgradeToCard(card, sourceBuffId);
            if (card.cardId == originalCardId)
            {
                continue;
            }

            hasChanges = true;
            changedCards?.Add(card);
        }

        return hasChanges;
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
}
