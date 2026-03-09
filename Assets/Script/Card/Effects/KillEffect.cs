using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KillEffect : ICardEffect
{
    public TargetType target = TargetType.SingleEnemy;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        ExecuteInternal(battleManager);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager)
    {
        if (battleManager == null) {
            return;
        }

        List<Monster> targets = ResolveTargets(battleManager);
        if (targets.Count == 0) {
            return;
        }

        foreach (Monster monster in targets) {
            if (monster == null || monster.IsDead()) {
                continue;
            }

            monster.Kill();
        }

        battleManager.UpdateAllUI();
    }

    private List<Monster> ResolveTargets(TrainingBattleManager battleManager)
    {
        List<Monster> targets = new List<Monster>();

        switch (target)
        {
            case TargetType.SingleEnemy:
                Monster singleTarget = battleManager.currentTarget;
                if (singleTarget == null) {
                    List<Monster> livingMonsters = battleManager.GetLivingMonsters();
                    if (livingMonsters != null && livingMonsters.Count == 1) {
                        singleTarget = livingMonsters[0];
                    }
                }

                if (singleTarget == null || singleTarget.IsDead()) {
                    Debug.LogWarning("[KillEffect] SingleEnemy 대상이 없습니다");
                    return targets;
                }

                targets.Add(singleTarget);
                return targets;

            case TargetType.AllEnemies:
                List<Monster> livingTargets = battleManager.GetLivingMonsters();
                if (livingTargets == null || livingTargets.Count == 0) {
                    Debug.LogWarning("[KillEffect] 처치할 적이 없습니다");
                    return targets;
                }

                targets.AddRange(livingTargets);
                return targets;

            default:
                Debug.LogWarning($"[KillEffect] 지원하지 않는 대상 타입입니다: {target}");
                return targets;
        }
    }
}
