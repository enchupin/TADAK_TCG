using System.Collections.Generic;
using UnityEngine;

// R-70 의도 큐 조작 카드 효과
// Delay: 무작위 amount ~ count칸 미루기 (102064 · 303064) · CopyAttack: 큐에서 가장 빠른 공격 의도 복사 (도발 303035 · 303060)
// Seal: 큐 맨 앞에 행동 없음 1칸, 보스는 대신 피해 15 (302074)
public class IntentQueueEffect : ICardEffect
{
    public const string Delay = "Delay";
    public const string CopyAttack = "CopyAttack";
    public const string Seal = "Seal";

    public TargetType target = TargetType.SingleEnemy;
    public string operation;
    public int amount = 1;
    // Delay 최대 칸 수 (0이면 amount 고정)
    public int maxAmount;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        foreach (Monster monster in ResolveTargets(battleManager))
        {
            if (!(monster is DataMonster dataMonster) || monster.IsDead())
            {
                continue;
            }

            switch (operation)
            {
                case Delay:
                    int minCount = Mathf.Max(1, amount);
                    int maxCount = Mathf.Max(minCount, maxAmount);
                    dataMonster.DelayIntent(Random.Range(minCount, maxCount + 1));
                    break;
                case CopyAttack:
                    dataMonster.CopyEarliestAttackIntent();
                    break;
                case Seal:
                    dataMonster.SealNextAction();
                    break;
                default:
                    Debug.LogWarning($"[IntentQueueEffect] 지원하지 않는 operation입니다: {operation}");
                    break;
            }
        }

        battleManager.UpdateAllUI();
    }

    private List<Monster> ResolveTargets(TrainingBattleManager battleManager)
    {
        if (target == TargetType.AllEnemies)
        {
            return battleManager.GetLivingMonsters() ?? new List<Monster>();
        }

        List<Monster> targets = new List<Monster>();
        Monster singleTarget = target == TargetType.RandomEnemy
            ? BuffCardUtility.PickRandomLivingMonster(battleManager)
            : CardEffectRuntimeUtility.ResolveSingleEnemyTarget(battleManager);
        if (singleTarget != null)
        {
            targets.Add(singleTarget);
        }

        return targets;
    }
}
