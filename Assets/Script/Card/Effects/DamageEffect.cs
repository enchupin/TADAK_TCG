using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 데미지 효과
/// 적에게 데미지를 입힙니다.
/// </summary>
[System.Serializable]
public class DamageEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.SingleEnemy;
    public ICardEffect onAction;
    
    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount
            : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);
        int totalDamageDealt = 0;
        
        if (battleManager.spawnedMonsters.Count > 0) // 몬스터가 존재할 때
        {
            switch (target)
            {
                case TargetType.SingleEnemy: // 단일 몬스터
                    Monster singleTarget = battleManager.spawnedMonsters[0];
                    if (singleTarget != null) {
                        totalDamageDealt += singleTarget.TakeDamage(finalAmount, battleManager.playerData.strength);
                    }
                    break;
                case TargetType.AllEnemies: // 몬스터 전체
                    List<Monster> allMonsters = new List<Monster>(battleManager.spawnedMonsters);
                    foreach (var m in allMonsters) {
                        totalDamageDealt += m.TakeDamage(finalAmount, battleManager.playerData.strength);
                    }
                    break;
            }
        }

        battleManager.battleContext.OnDamageDealt(totalDamageDealt);

        if (onAction != null) {
            onAction.Execute(battleManager);
        }
        
        // UI 업데이트는 왜 하는지 모르겠음, 추후 확인
        battleManager.UpdateAllUI();
    }
    
}

/// <summary>
/// 타겟 타입 열거형
/// </summary>
public enum TargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    AllAllies,
    Hand,
    Discard,
    Deck,
    None
}
