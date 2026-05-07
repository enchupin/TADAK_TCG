using System.Collections.Generic;
using UnityEngine;

public class FlashyScytheMonster : Monster
{
    private int patternIndex;

    public override int MonsterId => 105;
    protected override string MonsterName => "?꾨?????";
    protected override int BaseMaxHp => 21;

    protected override void BuildNextAction()
    {
        switch (patternIndex)
        {
            case 0:
                int strengthAmount = PreviewMonsterBuffAmount(BattleRuntimeDefinitions.StrengthBuffId, 2);
                SetIntent($"모든 아군이 힘을 {strengthAmount} 얻습니다.");
                SetPlannedPattern(10501, MonsterIntentIconType.BeneficialEffect);
                break;
            case 1:
                SetIntent("????由ы띁?먭쾶 蹂댄샇留됱쓣 7 遺?ы빀?덈떎.");
                SetPlannedPattern(10502, MonsterIntentIconType.Protection);
                break;
            default:
                int previewDamage = PreviewOutgoingDamage(4);
                SetAttackIntent(previewDamage, $"?쇳빐瑜?{previewDamage} ?낇옓?덈떎.");
                SetPlannedPattern(10503, MonsterIntentIconType.Attack);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (patternIndex)
        {
            case 0:
                ApplyStrengthToAllAllies();
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

    private void ApplyStrengthToAllAllies()
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
            monster?.AddBuff(BattleRuntimeDefinitions.StrengthBuffId, 2);
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
