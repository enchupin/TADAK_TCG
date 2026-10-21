using System.Collections.Generic;
using UnityEngine;
using static BattleRuntimeDefinitions;

public class BattleBuffController
{
    private readonly TrainingBattleManager battleManager;
    private readonly PlayerBuffRuntimeService playerBuffRuntimeService;
    private readonly MonsterBuffRuntimeService monsterBuffRuntimeService;
    private readonly FeatherBuffScript featherBuffScript;

    // 개별 알림과 계산은 담당 실행기로 직접 전달
    public PlayerBuffRuntimeService Player => playerBuffRuntimeService;
    public MonsterBuffRuntimeService Monsters => monsterBuffRuntimeService;

    public BattleBuffController(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
        playerBuffRuntimeService = new PlayerBuffRuntimeService(battleManager);
        monsterBuffRuntimeService = new MonsterBuffRuntimeService(battleManager);
        featherBuffScript = new FeatherBuffScript(battleManager, playerBuffRuntimeService);
    }

    public void ResetForCombat()
    {
        playerBuffRuntimeService.ResetForCombat();
    }

    public void ApplyPlayerTurnStartEffects()
    {
        playerBuffRuntimeService.OnPlayerTurnStart();
    }

    public void ApplyPlayerTurnEndEffects()
    {
        playerBuffRuntimeService.PreparePlayerTurnEnd();
        playerBuffRuntimeService.ReplayTurnEndTriggeredEffects();
    }

    public void FinishPlayerTurnEndEffects()
    {
        playerBuffRuntimeService.OnPlayerTurnEnd();
        monsterBuffRuntimeService.OnPlayerTurnEnd();
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
            int dealtDamage = monster.TakeDamage(battleManager.ResolvePlayerEffectDamage(damage), 0);
            totalDamageDealt += dealtDamage;
            Player.OnPlayerDamageDealt(monster, dealtDamage);
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
    }

    public int ResolvePlayerBarrierGain(int amount, bool fromCard = false)
    {
        int baseAmount = Mathf.Max(0, amount) + (fromCard ? Player.GetAdditionalBarrierGain() : 0);
        return playerBuffRuntimeService.ResolveBarrierGain(baseAmount);
    }

    public bool ShouldRetainPlayerBarrierOnTurnStart()
    {
        return Player.HasPermanentBarrierRetention() || playerBuffRuntimeService.TryConsumeBarrierRetentionOnTurnStart();
    }

    public int ResolvePlayerIncomingDamage(int damage, Monster attacker)
    {
        int safeDamage = Mathf.Max(0, damage);
        float multiplier = playerBuffRuntimeService.GetIncomingDamageMultiplier(attacker);
        int finalDamage = Mathf.FloorToInt(safeDamage * multiplier);
        return playerBuffRuntimeService.ClampIncomingDamage(attacker, finalDamage);
    }

    public int ResolvePersistentUpgradeCardId(int cardId, int sourceBuffId = 0)
    {
        int resolvedCardId = playerBuffRuntimeService.ResolvePersistentUpgradeCardId(cardId, sourceBuffId);
        return monsterBuffRuntimeService.ResolvePersistentUpgradeCardId(resolvedCardId, sourceBuffId);
    }

    public void RegisterMonsterHpLossHealPlayerThisTurn(Monster monster)
    {
        if (monster == null || monster.IsDead())
        {
            return;
        }

        battleManager.ApplyBuffToMonster(monster, LifeLinkBuffId, 1);
    }

    public void HandlePlayerAttackResolved(Monster targetMonster, int barrierBefore, int barrierAfter)
    {
        monsterBuffRuntimeService.OnAttackedByPlayer(targetMonster);
        playerBuffRuntimeService.OnPlayerAttackResolved(targetMonster, barrierBefore, barrierAfter);
    }

    public int TriggerFeather(TargetType target, int repeatCount = 1)
    {
        return featherBuffScript.Trigger(target, repeatCount);
    }

    public int TriggerFeatherUntilEmpty(TargetType target)
    {
        return featherBuffScript.TriggerUntilEmpty(target);
    }

    public System.Collections.IEnumerator ReplayExhaustedFeathersSequence()
    {
        return featherBuffScript.ReplayExhaustedFeathersSequence();
    }

    // 특별 적용과 일반 저장, 적용 후 알림을 같은 진입점에서 처리
    public void ApplyToPlayer(int buffId, int amount)
    {
        if (amount < 0)
        {
            ApplyNegativeAmountToPlayer(buffId, amount);
            return;
        }

        if (amount <= 0) return;

        if (!featherBuffScript.TryApplyToPlayer(buffId, amount)
            && !Player.TryApplyToPlayer(buffId, amount))
        {
            battleManager.playerData?.AddBuff(buffId, amount);
        }
        HandleBuffApplied(buffId);
    }

    public void ApplyToMonster(Monster monster, int buffId, int amount)
    {
        if (monster != null && !monster.IsDead() && amount < 0)
        {
            ApplyNegativeAmountToMonster(monster, buffId, amount);
            return;
        }

        if (monster == null || monster.IsDead() || amount <= 0) return;

        buffId = Player.ResolveAppliedMonsterBuffId(monster, buffId, amount);
        if (featherBuffScript.TryApplyToMonster(buffId, monster, amount))
        {
            HandleBuffApplied(buffId);
            return;
        }

        int crueltyStackBeforeApply = monster.GetBuffStack(CrueltyDebuffId);
        monster.AddBuff(buffId, amount);
        HandleBuffApplied(buffId);
        if (BuffData.IsHarmfulBuffId(buffId))
        {
            battleManager.HandleEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
        }
    }

    // 음수 부여는 음수 중첩 허용 효과(힘 등)만 적용하고 중첩 불가 효과는 제거, 그 외는 무시
    // 해로운 효과 부여 알림이나 깃털 특수 처리는 하지 않음
    private void ApplyNegativeAmountToPlayer(int buffId, int amount)
    {
        if (battleManager.playerData == null || !CanApplyNegativeAmount(buffId)) return;

        battleManager.playerData.AddBuff(buffId, amount);
    }

    private void ApplyNegativeAmountToMonster(Monster monster, int buffId, int amount)
    {
        if (!CanApplyNegativeAmount(buffId)) return;

        monster.AddBuff(buffId, amount);
    }

    private static bool CanApplyNegativeAmount(int buffId)
    {
        return BuffData.AllowsNegativeStack(buffId) || BuffData.IsNonStackableBuffId(buffId);
    }

    public void ApplyToAllEnemies(int buffId, int amount)
    {
        if (amount < 0)
        {
            foreach (Monster monster in battleManager.GetLivingMonsters())
            {
                ApplyNegativeAmountToMonster(monster, buffId, amount);
            }
            return;
        }

        if (amount <= 0) return;

        if (featherBuffScript.TryApplyToAllEnemies(buffId, amount))
        {
            HandleBuffApplied(buffId);
            return;
        }

        foreach (Monster monster in battleManager.GetLivingMonsters())
        {
            ApplyToMonster(monster, buffId, amount);
        }
    }

    public void HandleEnemyDebuffApplied(Monster monster, int buffId, int amount, int crueltyStackBeforeApply = -1)
    {
        if (!BuffData.IsHarmfulBuffId(buffId))
        {
            return;
        }

        monsterBuffRuntimeService.OnEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
        playerBuffRuntimeService.OnEnemyDebuffApplied(monster, buffId, amount, crueltyStackBeforeApply);
    }

    public void HandlePlayerHpLost(int hpLoss)
    {
        playerBuffRuntimeService.OnPlayerHpLost(hpLoss);
        monsterBuffRuntimeService.OnPlayerHpLost(hpLoss);
    }

    public int ResolveMonsterIncomingDamage(Monster monster, int damage)
    {
        int safeDamage = Mathf.Max(0, damage);
        float multiplier = monsterBuffRuntimeService.GetIncomingDamageMultiplier(monster);
        multiplier = ApplyHunterMultiplier(monster, multiplier);
        return Mathf.FloorToInt(safeDamage * multiplier);
    }

    // 사냥꾼 보유 시 기절·덫 상태인 적은 관통을 소모하지 않고 항상 관통 배율로 피해를 받음
    // 이미 관통을 보유했다면 그대로, 부식만 있다면 부식 대신 관통 배율 적용
    private float ApplyHunterMultiplier(Monster monster, float multiplier)
    {
        PlayerData player = battleManager.playerData;
        if (monster == null || player == null || player.GetBuffStack(HunterBuffId) <= 0
            || monster.GetBuffStack(EnhancedCorrosionBuffId) > 0
            || !(monster.IsStunned || monster.GetBuffStack(TrapStateBuffId) > 0))
        {
            return multiplier;
        }

        if (monster.GetBuffStack(CorrosionBuffId) > 0)
        {
            int corrosionAsBuffId = player.GetBuffStack(CorrosionEnhanceBuffId) > 0 ? EnhancedCorrosionBuffId : CorrosionBuffId;
            multiplier /= BuffValueUtility.GetIncomingDamageMultiplier(corrosionAsBuffId);
        }

        return multiplier * BuffValueUtility.GetIncomingDamageMultiplier(EnhancedCorrosionBuffId);
    }

    public void ConsumeMonsterIncomingDamageBuffs(Monster monster, int damage)
    {
        if (monster == null || damage <= 0)
        {
            return;
        }

        monsterBuffRuntimeService.TryConsumeIncomingDamageBuff(monster);
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
