using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 버프 효과
/// 플레이어에게 버프를 부여합니다.
/// </summary>
[System.Serializable]
public class BuffEffect : ICardEffect
{
    public int buffId;
    public int amount;
    public string amountFormula;
    public int duration;
    public TargetType target;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

        if (buffId <= 0)
        {
            Debug.LogWarning("[BuffEffect] buffId가 없는 Buff 이펙트는 지원하지 않습니다");
            return;
        }

        // Apply by BuffID using BuffManager
        ApplyBuff(battleManager, finalAmount);
        battleManager.UpdateAllUI();
    }

    private void ApplyBuff(TrainingBattleManager manager, int finalAmount)
    {
        if (target == TargetType.Self)
        {
            manager.ApplyBuffToPlayer(buffId, finalAmount);
        }
        else if (target == TargetType.SingleEnemy)
        {
            Monster targetMonster = manager.currentTarget;
            if (targetMonster == null)
            {
                List<Monster> livingMonsters = manager.GetLivingMonsters();
                if (livingMonsters != null && livingMonsters.Count == 1)
                {
                    targetMonster = livingMonsters[0];
                }
            }

            if (targetMonster != null && !targetMonster.IsDead())
            {
                manager.ApplyBuffToMonster(targetMonster, buffId, finalAmount);
            }
        }
        else if (target == TargetType.AllEnemies)
        {
            List<Monster> livingMonsters = manager.GetLivingMonsters();
            if (livingMonsters == null)
                return;

            foreach (Monster monster in livingMonsters)
            {
                if (monster != null && !monster.IsDead())
                {
                    manager.ApplyBuffToMonster(monster, buffId, finalAmount);
                }
            }
        }
    }
}
