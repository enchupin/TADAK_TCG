using System.Collections.Generic;
using UnityEngine;

public class FlashyScytheMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 105;
    protected override string MonsterName => "현란한 낫";
    protected override int BaseMaxHp => 21;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                SetIntent("모든 아군이 피해 증폭을 2 얻습니다.");
                SetPlannedPattern(10501, MonsterIntentIconType.BeneficialEffect);
                break;
            case 1:
                SetIntent("잭 오 리퍼에게 보호막을 7 부여합니다.");
                SetPlannedPattern(10502, MonsterIntentIconType.Protection);
                break;
            default:
                SetAttackIntent(4, "피해를 4 입힙니다.");
                SetPlannedPattern(10503, MonsterIntentIconType.Attack);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                ApplyDamageAmplifyToAllAllies();
                break;
            case 1:
                FindJackORipper()?.AddDefense(7);
                break;
            default:
                DealDamage(target, 4);
                break;
        }

        patternIndex = GetNextPatternIndex();
    }

    private void ApplyDamageAmplifyToAllAllies()
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
            monster?.AddBuff(BattleRuntimeDefinitions.DamageAmplifyBuffId, 2);
        }
    }

    private Monster FindJackORipper()
    {
        List<Monster> livingMonsters = TrainingBattleManager.Instance != null
            ? TrainingBattleManager.Instance.GetLivingMonsters()
            : null;
        if (livingMonsters == null)
        {
            return null;
        }

        foreach (Monster monster in livingMonsters)
        {
            if (monster != null && !monster.IsDead() && monster.MonsterId == 104)
            {
                return monster;
            }
        }

        return null;
    }

    private int GetNextPatternIndex()
    {
        List<int> patternCandidates = new List<int>(2);
        for (int candidateIndex = 0; candidateIndex < 3; candidateIndex++)
        {
            if (candidateIndex != patternIndex)
            {
                patternCandidates.Add(candidateIndex);
            }
        }

        return patternCandidates[Random.Range(0, patternCandidates.Count)];
    }
}
