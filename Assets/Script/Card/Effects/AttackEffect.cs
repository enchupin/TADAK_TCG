using UnityEngine;
using System.Collections.Generic;

public class AttackEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target;
    public ICardEffect onAction;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = EffectAmountResolver.Resolve(amount, amountFormula, battleManager.battleContext, battleManager.playerData); // 유닛 당 데미지
        int totalDamageDealt = 0; // 총 누적 데미지

        if (battleManager.spawnedMonsters.Count > 0) {
            if (target == TargetType.AllEnemies) { // 모든 적에게 데미지
                List<Monster> targets = new(battleManager.spawnedMonsters);
                if (targets == null) {
                    Debug.LogWarning("[AttackEffect] EnemiesList is missing. Effect cancelled.");
                    return;
                }
                foreach (var monster in targets) {
                    totalDamageDealt += monster.TakeDamage(finalAmount, battleManager.playerData.strength);
                }
            }
            else if (target == TargetType.SingleEnemy) { // 단일 적에게 데미지
                Monster targetMonster = battleManager.currentTarget;
                if (targetMonster == null) {
                    Debug.LogWarning("[AttackEffect] SingleEnemy target is missing. Effect cancelled.");
                    return;
                }
                totalDamageDealt += targetMonster.TakeDamage(finalAmount, battleManager.playerData.strength);
            }
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);

        onAction?.Execute(battleManager);
    }
}
