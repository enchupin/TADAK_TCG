using UnityEngine;
using System.Collections.Generic;

public class AttackEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<int> cardIdList;
    public TargetType target;
    public ICardEffect onAction;

    public void Execute(TrainingBattleManager battleManager)
    {
        // 유닛 당 데미지
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount
            : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList, amount);
        int totalDamageDealt = 0; // 총 누적 데미지

        switch (target) {
            case TargetType.AllEnemies: // 모든 적에게 데미지
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[AttackEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> targets = new(battleManager.spawnedMonsters);
                if (targets == null) {
                    Debug.LogWarning("[AttackEffect] EnemiesList is missing. Effect cancelled.");
                    return;
                }

                foreach (var monster in targets) {
                    totalDamageDealt += monster.TakeDamage(finalAmount, battleManager.playerData.strength);
                }
                break;

            case TargetType.SingleEnemy: // 단일 적에게 데미지
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[AttackEffect] No enemies available for SingleEnemy target. Effect cancelled.");
                    break;
                }

                Monster targetMonster = battleManager.currentTarget;
                if (targetMonster == null) {
                    // If only one enemy exists, auto-select it.
                    List<Monster> livingMonsters = battleManager.GetLivingMonsters();
                    if (livingMonsters != null && livingMonsters.Count == 1) {
                        targetMonster = livingMonsters[0];
                    }
                }
                if (targetMonster == null) {
                    Debug.LogWarning("[AttackEffect] SingleEnemy target is missing. Effect cancelled.");
                    return;
                }

                totalDamageDealt += targetMonster.TakeDamage(finalAmount, battleManager.playerData.strength);
                break;

            case TargetType.Self: // 자신에게 데미지
                if (battleManager.playerData == null) {
                    Debug.LogWarning("[AttackEffect] PlayerData is missing. Self target effect cancelled.");
                    return;
                }

                battleManager.playerData.TakeDamage(finalAmount);
                break;
        }

        battleManager.battleContext?.OnDamageDealt(totalDamageDealt);
        if (totalDamageDealt > 0 && battleManager.battleContext != null)
        {
            Debug.Log($"[AttackEffect] Damage dealt: {totalDamageDealt}, LastDamage: {battleManager.battleContext.lastDamageDealt}, ThisTurnTotal: {battleManager.battleContext.totalDamageDealt}");
        }

        onAction?.Execute(battleManager, totalDamageDealt);
    }

}
