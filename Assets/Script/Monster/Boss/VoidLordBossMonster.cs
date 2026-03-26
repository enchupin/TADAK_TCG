using System.Collections.Generic;
using UnityEngine;

public class VoidLordBossMonster : Monster
{
    private int nextPatternId;
    private int pendingAttackBoost;
    private bool hasTriggeredVoidShellThisTurn;
    private bool isInvulnerable;
    private bool recoverySequenceActive;

    public override int MonsterId => 304;
    protected override string MonsterName => "\uACF5\uD5C8 \uAD70\uC8FC";
    protected override int BaseMaxHp => 400;
    protected override bool IsBossMonster => true;

    protected override void OnBattleStart()
    {
        nextPatternId = 30403;
        pendingAttackBoost = 0;
        hasTriggeredVoidShellThisTurn = false;
        isInvulnerable = false;
        recoverySequenceActive = false;
        AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, 11);
    }

    protected override void OnTurnStarted()
    {
        hasTriggeredVoidShellThisTurn = false;
    }

    protected override void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
        if (incomingDamage <= 0 || hasTriggeredVoidShellThisTurn || IsDead())
        {
            return;
        }

        int voidShellStack = GetBuffStack(BattleRuntimeDefinitions.VoidShellBuffId);
        if (voidShellStack <= 0)
        {
            return;
        }

        hasTriggeredVoidShellThisTurn = true;
        AddDefense(voidShellStack);
    }

    protected override void OnTurnEnded()
    {
        if (IsDead() || recoverySequenceActive)
        {
            return;
        }

        if (GetNegativeBuffTypeCount() < 3)
        {
            return;
        }

        nextPatternId = 30404;
    }

    protected override bool CanReceiveDamage(int incomingDamage)
    {
        return !isInvulnerable;
    }

    protected override void BuildNextAction()
    {
        switch (nextPatternId)
        {
            case 30401:
                SetIntent("\uBF51\uC744 \uCE74\uB4DC \uB354\uBBF8, \uC190, \uBC84\uB9B0 \uCE74\uB4DC \uB354\uBBF8\uC5D0 \uACF5\uD5C8\uC758 \uBD80\uB984 \uCE74\uB4DC\uB97C \uAC01\uAC01 1\uC7A5\uC529 \uC0DD\uC131\uD569\uB2C8\uB2E4.");
                SetPlannedPattern(30401, MonsterIntentIconType.DisruptCard);
                break;
            case 30402:
                SetIntent("\uBAA8\uB4E0 \uC544\uAD70\uC774 \uACF5\uD5C8 \uAF8D\uC9C8\uC744 4 \uC5BB\uC2B5\uB2C8\uB2E4.");
                SetPlannedPattern(30402, MonsterIntentIconType.BeneficialEffect);
                break;
            case 30403:
                int previewDamage = GetPreviewDamage(14);
                SetAttackIntent(previewDamage, $"\uD53C\uD574\uB97C {previewDamage}\uC529 3\uD68C \uC785\uD799\uB2C8\uB2E4.");
                SetPlannedPattern(30403, MonsterIntentIconType.Attack);
                break;
            case 30404:
                SetIntent("\uD574\uB85C\uC6B4 \uD6A8\uACFC\uB97C \uBAA8\uB450 \uC81C\uAC70\uD569\uB2C8\uB2E4. \uB2E4\uC74C \uD134\uC5D0 \uC544\uBB34\uAC83\uB3C4 \uD558\uC9C0 \uC54A\uC9C0\uB9CC, \uD53C\uD574\uB97C \uBC1B\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.");
                SetPlannedPattern(30404, MonsterIntentIconType.BeneficialEffect);
                break;
            case 30405:
                SetIntent("\uC544\uBB34\uAC83\uB3C4 \uD558\uC9C0 \uC54A\uC2B5\uB2C8\uB2E4.");
                SetPlannedPattern(30405, MonsterIntentIconType.Stun);
                break;
            default:
                SetIntent("\uACF5\uACA9 \uAC15\uD654\uB97C 6 \uC5BB\uC2B5\uB2C8\uB2E4.");
                SetPlannedPattern(30406, MonsterIntentIconType.BeneficialEffect);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (nextPatternId)
        {
            case 30401:
                AddVoidCallCards();
                nextPatternId = 30402;
                break;
            case 30402:
                ApplyVoidShellToAllAllies(4);
                nextPatternId = 30403;
                break;
            case 30403:
                ExecuteTripleAttack(target);
                nextPatternId = DetermineStandardLoopStartPattern();
                if (recoverySequenceActive)
                {
                    recoverySequenceActive = false;
                }
                break;
            case 30404:
                RemoveAllNegativeBuffs();
                isInvulnerable = true;
                recoverySequenceActive = true;
                nextPatternId = 30405;
                break;
            case 30405:
                isInvulnerable = false;
                nextPatternId = 30403;
                break;
            default:
                pendingAttackBoost += 6;
                nextPatternId = 30403;
                break;
        }
    }

    private void ExecuteTripleAttack(PlayerData target)
    {
        int boostedDamage = 14 + pendingAttackBoost;
        for (int hitIndex = 0; hitIndex < 3; hitIndex++)
        {
            DealDamage(target, boostedDamage);
            if (target != null && target.IsDead())
            {
                break;
            }
        }

        pendingAttackBoost = 0;
    }

    private void AddVoidCallCards()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null)
        {
            return;
        }

        Card drawPileCard = CreateVoidCallCard();
        Card handCard = CreateVoidCallCard();
        Card discardCard = CreateVoidCallCard();

        battleManager.usableDeckManager?.AddToDrawPileRandom(drawPileCard);
        battleManager.handManager?.AddCard(handCard);
        battleManager.usableDeckManager?.AddToDiscard(discardCard);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private Card CreateVoidCallCard()
    {
        List<CardData> allCards = CardManager.GetAllCards();
        foreach (CardData cardData in allCards)
        {
            if (cardData != null && cardData.cardId == BattleRuntimeDefinitions.VoidCallCardId)
            {
                return cardData.ToCard();
            }
        }

        Card fallbackCard = new Card
        {
            cardId = BattleRuntimeDefinitions.VoidCallCardId,
            cardName = "\uACF5\uD5C8\uC758 \uBD80\uB984",
            character = 0,
            cost = 0,
            description = "\uD134 \uC885\uB8CC \uC2DC \uC57D\uD654\uB97C 2 \uC5BB\uC2B5\uB2C8\uB2E4.",
            keywords = new List<int> { CardKeywordIds.Unplayable },
            effects = new List<ICardEffect>(),
            keepEffects = new List<ICardEffect>(),
            endTurnInHandEffects = new List<ICardEffect>
            {
                new BuffEffect
                {
                    buffId = BattleRuntimeDefinitions.WeakBuffId,
                    amount = 2,
                    target = TargetType.Self
                }
            }
        };
        fallbackCard.InitializeRuntimeState();
        return fallbackCard;
    }

    private void ApplyVoidShellToAllAllies(int amount)
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        if (livingMonsters == null)
        {
            return;
        }

        foreach (Monster monster in livingMonsters)
        {
            monster?.AddBuff(BattleRuntimeDefinitions.VoidShellBuffId, amount);
        }
    }

    private void RemoveAllNegativeBuffs()
    {
        currentBuffs.RemoveAll(buff =>
            buff?.data != null && !BuffData.IsBeneficialBuffId(buff.data.buffId));
    }

    private int DetermineStandardLoopStartPattern()
    {
        return HasOtherLivingAllies() ? 30401 : 30406;
    }

    private bool HasOtherLivingAllies()
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        if (livingMonsters == null)
        {
            return false;
        }

        foreach (Monster monster in livingMonsters)
        {
            if (monster == null || monster == this || monster.IsDead())
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private int GetNegativeBuffTypeCount()
    {
        HashSet<int> negativeBuffIds = new HashSet<int>();
        foreach (Buff buff in currentBuffs)
        {
            if (buff?.data == null)
            {
                continue;
            }

            if (BuffData.IsBeneficialBuffId(buff.data.buffId))
            {
                continue;
            }

            negativeBuffIds.Add(buff.data.buffId);
        }

        return negativeBuffIds.Count;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + pendingAttackBoost + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }
}
