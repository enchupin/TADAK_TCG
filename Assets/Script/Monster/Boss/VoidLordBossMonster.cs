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
    protected override string MonsterName => "공허 군주";
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
                SetIntent("뽑을 카드 더미, 손, 버린 카드 더미에 공허의 부름 카드를 각각 1장씩 생성합니다.");
                SetPlannedPattern(30401, MonsterIntentIconType.DisruptCard);
                break;
            case 30402:
                SetIntent("모든 아군이 공허 껍질을 4 얻습니다.");
                SetPlannedPattern(30402, MonsterIntentIconType.BeneficialEffect);
                break;
            case 30403:
                int previewDamage = GetPreviewDamage(14);
                SetAttackIntent(previewDamage, $"피해를 {previewDamage}씩 3회 입힙니다.");
                SetPlannedPattern(30403, MonsterIntentIconType.Attack);
                break;
            case 30404:
                SetIntent("해로운 효과를 모두 제거합니다. 다음 턴에 아무것도 하지 않지만, 피해를 받지 않습니다.");
                SetPlannedPattern(30404, MonsterIntentIconType.BeneficialEffect);
                break;
            case 30405:
                SetIntent("아무것도 하지 않습니다.");
                SetPlannedPattern(30405, MonsterIntentIconType.Stun);
                break;
            default:
                SetIntent("공격 강화를 6 얻습니다.");
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

        Card drawPileCard = CardManager.GetCardAsCard(30);
        Card handCard = CardManager.GetCardAsCard(30);
        Card discardCard = CardManager.GetCardAsCard(30);

        if (drawPileCard != null)
        {
            battleManager.usableDeckManager?.AddToDrawPileRandom(drawPileCard);
        }

        if (handCard != null)
        {
            battleManager.handManager?.AddCard(handCard);
        }

        if (discardCard != null)
        {
            battleManager.usableDeckManager?.AddToDiscard(discardCard);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
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
