using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// 피해 효과
/// 대상을 지정해 피해를 가합니다.
/// </summary>
[System.Serializable]
public class DamageEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public TargetType target = TargetType.SingleEnemy;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = BuildFinalDamageAmount(battleManager);
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

                totalDamageDealt += singleTarget.TakeDamage(finalAmount, 0);
                break;

            case TargetType.AllEnemies: // 모든 적 대상
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[DamageEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> allMonsters = new List<Monster>(battleManager.spawnedMonsters);
                foreach (var m in allMonsters)
                {
                    totalDamageDealt += m.TakeDamage(finalAmount, 0);
                }
                break;

            case TargetType.Self: // 플레이어 자신 대상
                if (battleManager.playerData == null)
                {
                    Debug.LogWarning("[DamageEffect] PlayerData is missing. Self target effect cancelled.");
                    break;
                }

                battleManager.playerData.TakeDamage(finalAmount);
                break;
        }

        battleManager.battleContext.OnDamageDealt(totalDamageDealt);

        if (onActions != null) {
            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, totalDamageDealt);
            }
        }

        battleManager.UpdateAllUI();
    }

    private int BuildFinalDamageAmount(TrainingBattleManager battleManager)
    {
        float cardMultiplier = 1f;
        int baseAmount = amount;

        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            if (TryParseMultiplierFormula(amountFormula, out float parsedMultiplier))
            {
                cardMultiplier = parsedMultiplier;
            }
            else
            {
                baseAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, null, amount);
            }
        }

        if (battleManager.playerData == null)
        {
            return Mathf.Max(0, baseAmount);
        }

        return battleManager.playerData.CalculateFinalDamage(baseAmount, cardMultiplier);
    }

    private bool TryParseMultiplierFormula(string formula, out float multiplier)
    {
        multiplier = 1f;
        if (string.IsNullOrWhiteSpace(formula))
            return false;

        string trimmed = formula.Trim();
        if (!trimmed.StartsWith("*"))
            return false;

        string numeric = trimmed.Substring(1);
        return float.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out multiplier);
    }
}

/// <summary>
/// 효과 대상 열거형
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
