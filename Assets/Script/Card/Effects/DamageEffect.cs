using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Damage effect
/// Deals damage to targets.
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

        switch (target)
        {
            case TargetType.SingleEnemy: // 단일 적 대상
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[DamageEffect] No enemies available for SingleEnemy target. Effect cancelled.");
                    break;
                }

                Monster singleTarget = battleManager.currentTarget;
                if (singleTarget == null) {
                    // If only one enemy exists, auto-select it.
                    List<Monster> livingMonsters = battleManager.GetLivingMonsters();
                    if (livingMonsters != null && livingMonsters.Count == 1) {
                        singleTarget = livingMonsters[0];
                    }
                }
                if (singleTarget == null) {
                    Debug.LogWarning("[DamageEffect] SingleEnemy target is missing. Effect cancelled.");
                    break;
                }

                totalDamageDealt += singleTarget.TakeDamage(finalAmount, battleManager.playerData.strength);
                break;

            case TargetType.AllEnemies: // 모든 적 대상
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[DamageEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> allMonsters = new List<Monster>(battleManager.spawnedMonsters);
                foreach (var m in allMonsters)
                {
                    totalDamageDealt += m.TakeDamage(finalAmount, battleManager.playerData.strength);
                }
                break;

            case TargetType.Self: // 플레이어 대상
                if (battleManager.playerData == null)
                {
                    Debug.LogWarning("[DamageEffect] PlayerData is missing. Self target effect cancelled.");
                    break;
                }

                battleManager.playerData.TakeDamage(finalAmount);
                break;
        }

        battleManager.battleContext.OnDamageDealt(totalDamageDealt);

        if (onAction != null)
        {
            onAction.Execute(battleManager);
        }

        battleManager.UpdateAllUI();
    }
}

/// <summary>
/// Effect target enum
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
