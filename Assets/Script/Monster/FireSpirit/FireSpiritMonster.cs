using System.Collections.Generic;
using UnityEngine;

public class FireSpiritMonster : Monster
{
    private const int ReviveDelayTurns = 2;

    private int plannedPatternIdForTurn;
    private int lastExecutedPatternId;

    public override int MonsterId => 111;
    protected override string MonsterName => "불꽃 정령";
    protected override int BaseMaxHp => 34;

    protected override void OnBattleStart()
    {
        plannedPatternIdForTurn = 0;
        lastExecutedPatternId = 0;
        ApplyBurningFlameBuff();
    }

    protected override bool IsNonStackableBuff(int buffId)
    {
        return buffId == BattleRuntimeDefinitions.BurningFlameBuffId || base.IsNonStackableBuff(buffId);
    }

    protected override void OnDeathTriggered()
    {
        if (!HasOtherLivingFireSpirit())
        {
            return;
        }

        TrainingBattleManager.Instance?.ScheduleMonsterRevive(this, ReviveDelayTurns);
    }

    protected override void OnRevivedTriggered()
    {
        plannedPatternIdForTurn = 0;
        lastExecutedPatternId = 0;
        ApplyBurningFlameBuff();
    }

    protected override void BuildNextAction()
    {
        plannedPatternIdForTurn = SelectNextPatternId();

        switch (plannedPatternIdForTurn)
        {
            case 11101:
                SetIntent("피해 증폭을 2 얻습니다.");
                SetPlannedPattern(11101, MonsterIntentIconType.BeneficialEffect);
                break;
            case 11102:
                SetAttackIntent(GetPreviewDamage(3), $"피해를 {GetPreviewDamage(3)}씩 2회 입힙니다.");
                SetPlannedPattern(11102, MonsterIntentIconType.Attack);
                break;
            default:
                SetAttackIntent(GetPreviewDamage(12), $"피해를 {GetPreviewDamage(12)} 입힙니다.");
                SetPlannedPattern(11103, MonsterIntentIconType.Attack);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (plannedPatternIdForTurn)
        {
            case 11101:
                AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 2);
                break;
            case 11102:
                for (int hitIndex = 0; hitIndex < 2; hitIndex++)
                {
                    DealDamage(target, 3);
                    if (target != null && target.IsDead())
                    {
                        break;
                    }
                }
                break;
            default:
                DealDamage(target, 12);
                break;
        }

        lastExecutedPatternId = plannedPatternIdForTurn;
    }

    private void ApplyBurningFlameBuff()
    {
        AddBuff(BattleRuntimeDefinitions.BurningFlameBuffId, 1);
    }

    private int SelectNextPatternId()
    {
        List<int> candidates = new List<int> { 11101, 11102, 11103 };
        if (lastExecutedPatternId == 11101)
        {
            candidates.Remove(11101);
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool HasOtherLivingFireSpirit()
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

            if (monster.MonsterId == MonsterId)
            {
                return true;
            }
        }

        return false;
    }

    private int GetPreviewDamage(int baseDamage)
    {
        return Mathf.Max(0, baseDamage + GetBuffStack(BattleRuntimeDefinitions.DamageAmplifyBuffId));
    }
}
