using System.Collections.Generic;
using UnityEngine;

public class GiantFlowerSpiderBossMonster : Monster
{
    private const int MaxLegCount = 8;

    private int plannedPatternIdForTurn;
    private int lastPatternId;
    private int accumulatedHpLoss;
    private bool hasUsedOpeningPattern;

    public override int MonsterId => 301;
    protected override string MonsterName => "큰꽃거미";
    protected override int BaseMaxHp => 450;
    protected override bool IsBossMonster => true;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.EightLegsBuffId, MaxLegCount);
        plannedPatternIdForTurn = 0;
        lastPatternId = 0;
        accumulatedHpLoss = 0;
        hasUsedOpeningPattern = false;
    }

    protected override void OnAfterTakeDamage(int incomingDamage, int damageAfterDefense)
    {
        if (damageAfterDefense <= 0)
        {
            return;
        }

        accumulatedHpLoss += damageAfterDefense;

        int targetLostLegCount = Mathf.Min(MaxLegCount, accumulatedHpLoss / 50);
        int currentLegCount = Mathf.Max(0, GetBuffStack(BattleRuntimeDefinitions.EightLegsBuffId));
        int desiredLegCount = Mathf.Max(0, MaxLegCount - targetLostLegCount);
        int legLossCount = Mathf.Max(0, currentLegCount - desiredLegCount);
        if (legLossCount <= 0)
        {
            return;
        }

        ConsumeBuffStack(BattleRuntimeDefinitions.EightLegsBuffId, legLossCount);
        AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, legLossCount * 3);
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = SelectNextPatternId();

        switch (plannedPatternIdForTurn)
        {
            case 30101:
                int legCount = GetLegCount();
                int legHitDamage = legCount > 0 ? GetPreviewDamage(2) : 0;
                SetAttackIntent(legHitDamage, $"피해를 {legHitDamage}씩 {legCount}회 입힙니다.");
                SetPlannedPattern(30101, MonsterIntentIconType.Attack);
                break;
            case 30102:
                SetIntent("적에게 약화, 부식, 빈약을 3씩 부여합니다.");
                SetPlannedPattern(30102, MonsterIntentIconType.HarmfulEffect);
                break;
            case 30103:
                SetAttackIntent(GetPreviewDamage(21), $"피해를 {GetPreviewDamage(21)} 입힙니다. 버린 카드 더미에 자상을 2장 생성합니다.");
                SetPlannedPattern(30103, MonsterIntentIconType.Attack, MonsterIntentIconType.DisruptCard);
                break;
            default:
                SetAttackIntent(GetPreviewDamage(8), $"보호막을 8 얻습니다. 피해를 {GetPreviewDamage(8)} 입힙니다. 체력을 8 회복합니다.");
                SetPlannedPattern(30104, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection, MonsterIntentIconType.Heal);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (plannedPatternIdForTurn)
        {
            case 30101:
                int legCount = GetLegCount();
                for (int hitIndex = 0; hitIndex < legCount; hitIndex++)
                {
                    DealDamage(target, 2);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }
                break;
            case 30102:
                target?.AddBuff(BattleRuntimeDefinitions.WeakBuffId, 3);
                target?.AddBuff(BattleRuntimeDefinitions.CorrosionBuffId, 3);
                target?.AddBuff(BattleRuntimeDefinitions.FrailBuffId, 3);
                break;
            case 30103:
                DealDamage(target, 21);
                AddCardToPlayerDiscard(CardManager.GetCardAsCard(10));
                AddCardToPlayerDiscard(CardManager.GetCardAsCard(10));
                break;
            default:
                AddDefense(8);
                DealDamage(target, 8);
                Heal(8);
                break;
        }

        hasUsedOpeningPattern = true;
        lastPatternId = plannedPatternIdForTurn;
    }

    private int SelectNextPatternId()
    {
        if (!hasUsedOpeningPattern)
        {
            return 30101;
        }

        List<int> candidates = new List<int> { 30101, 30102, 30103, 30104 };
        candidates.Remove(lastPatternId);
        return candidates[Random.Range(0, candidates.Count)];
    }

    private int GetLegCount()
    {
        return Mathf.Max(0, GetBuffStack(BattleRuntimeDefinitions.EightLegsBuffId));
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }
}
