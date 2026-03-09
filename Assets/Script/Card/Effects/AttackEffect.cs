using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class AttackEffect : ICardEffect
{
    private const int DamageAmplifyBuffId = 3002;
    private const int OverheatBuffId = 3017;

    public int amount;
    public string amountFormula;
    public List<int> cardIdList;
    public TargetType target;
    public float ampMultiplier = 1f;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        // 최종 공격 피해
        int finalAmount = BuildFinalDamageAmount(battleManager);
        int totalDamageDealt = 0; // 총 누적 피해

        switch (target) {
            case TargetType.AllEnemies: // 모든 적에게 피해
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[AttackEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> targets = new(battleManager.spawnedMonsters);
                if (targets == null) {
                    Debug.LogWarning("[AttackEffect] EnemiesList is missing. Effect cancelled.");
                    return;
                }

                foreach (Monster monster in targets) {
                    totalDamageDealt += monster.TakeDamage(finalAmount, 0);
                }
                break;

            case TargetType.SingleEnemy: // 단일 적에게 피해
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

                totalDamageDealt += targetMonster.TakeDamage(finalAmount, 0);
                break;

            case TargetType.Self: // 자신에게 피해
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

        if (onActions != null) {
            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, totalDamageDealt);
            }
        }
    }

    private int BuildFinalDamageAmount(TrainingBattleManager battleManager)
    {
        ResolveAttackAmount(battleManager, out int baseAmount, out float cardMultiplier);

        int scaledBaseDamage = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
        if (battleManager.playerData == null)
        {
            return scaledBaseDamage;
        }

        int damageAmplify = Mathf.Max(0, Mathf.FloorToInt(
            battleManager.playerData.GetBuffStack(DamageAmplifyBuffId) * Mathf.Max(0f, ampMultiplier)));

        float overheatMultiplier = 1f + Mathf.Max(0, battleManager.playerData.GetBuffStack(OverheatBuffId)) * 0.1f;
        return Mathf.Max(0, Mathf.FloorToInt((scaledBaseDamage + damageAmplify) * overheatMultiplier));
    }

    private void ResolveAttackAmount(TrainingBattleManager battleManager, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = amount;

        if (string.IsNullOrWhiteSpace(amountFormula))
        {
            return;
        }

        if (TryParseMultiplierFormula(amountFormula, out float parsedMultiplier))
        {
            cardMultiplier = parsedMultiplier;
            return;
        }

        baseAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, cardIdList, amount);
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
