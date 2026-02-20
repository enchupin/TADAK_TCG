using UnityEngine;
using System.Collections.Generic;

public class AttackEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = amount;
        
        // Use FormulaEvaluator with BattleContext and PlayerData
        if (!string.IsNullOrEmpty(amountFormula))
        {
            finalAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        }

        if (battleManager.spawnedMonsters.Count > 0)
        {
            if (target == TargetType.AllEnemies)
            {
                // 모든 적에게 데미지
                // 리스트 복사본을 만들어 루프 중 몬스터가 죽어서 리스트가 변경되는 오류 방지
                List<Monster> targets = new List<Monster>(battleManager.spawnedMonsters);
                foreach (var monster in targets)
                {
                    monster.TakeDamage(finalAmount, battleManager.playerData.strength);
                }
            }
            else
            {
                // 단일 적 타겟팅 (마우스 드래그로 지정한 타겟이 있으면 사용, 없으면 첫 번째 몬스터)
                Monster targetMonster = battleManager.currentTarget;
                if (targetMonster == null && battleManager.spawnedMonsters.Count > 0)
                {
                    targetMonster = battleManager.spawnedMonsters[0];
                }
                
                if (targetMonster != null)
                {
                    targetMonster.TakeDamage(finalAmount, battleManager.playerData.strength);
                }
            }
        }
    }
}
