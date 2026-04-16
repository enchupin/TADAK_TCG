using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// 피해 효과
/// 대상을 지정해 피해를 가합니다
/// </summary>
[System.Serializable]
public class DamageEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public float ampMultiplier = 1f;
    public TargetType target = TargetType.SingleEnemy;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        ExecuteInternal(battleManager, amount);
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        ExecuteInternal(battleManager, amount);
    }

    private void ExecuteInternal(TrainingBattleManager battleManager, int forwardedAmount)
    {
        ResolveDamageAmount(battleManager, forwardedAmount, out int baseAmount, out float cardMultiplier);
        int finalAmount = BuildFinalDamageAmount(battleManager, baseAmount, cardMultiplier);
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

                int singleDamage = singleTarget.TakeDamage(finalAmount, 0);
                totalDamageDealt += singleDamage;
                battleManager.HandlePlayerDamageDealt(singleTarget, singleDamage);
                break;

            case TargetType.AllEnemies: // 모든 적 대상
                if (battleManager.spawnedMonsters.Count <= 0) {
                    Debug.LogWarning("[DamageEffect] No enemies available for AllEnemies target. Effect cancelled.");
                    break;
                }

                List<Monster> allMonsters = new List<Monster>(battleManager.spawnedMonsters);
                foreach (var m in allMonsters)
                {
                    int dealtDamage = m.TakeDamage(finalAmount, 0);
                    totalDamageDealt += dealtDamage;
                    battleManager.HandlePlayerDamageDealt(m, dealtDamage);
                }
                break;

            case TargetType.RandomEnemy:
                List<Monster> randomTargets = battleManager.GetLivingMonsters();
                if (randomTargets == null || randomTargets.Count == 0) {
                    Debug.LogWarning("[DamageEffect] No enemies available for RandomEnemy target. Effect cancelled.");
                    break;
                }

                Monster randomTarget = randomTargets[Random.Range(0, randomTargets.Count)];
                int randomDamage = randomTarget.TakeDamage(finalAmount, 0);
                totalDamageDealt += randomDamage;
                battleManager.HandlePlayerDamageDealt(randomTarget, randomDamage);
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

    private int BuildFinalDamageAmount(TrainingBattleManager battleManager, int baseAmount, float cardMultiplier)
    {
        Card sourceCard = battleManager?.battleContext?.GetLastPlayedCard();
        if (battleManager.playerData == null)
        {
            int fallbackDamage = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            return battleManager != null
                ? battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, fallbackDamage)
                : fallbackDamage;
        }

        int resolvedDamage = battleManager.playerData.CalculateCardDamage(baseAmount, ampMultiplier, cardMultiplier);
        return battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, resolvedDamage);
    }

    private void ResolveDamageAmount(TrainingBattleManager battleManager, int forwardedAmount, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = amount > 0 ? amount : Mathf.Max(0, forwardedAmount);
        int formulaBaseValue = forwardedAmount > 0 ? forwardedAmount : amount;

        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            if (TryParseMultiplierFormula(amountFormula, out float parsedMultiplier))
            {
                cardMultiplier = parsedMultiplier;
                return;
            }

            if (IsTargetHpFormula(amountFormula))
            {
                Monster targetMonster = ResolveCurrentTarget(battleManager);
                baseAmount = targetMonster != null ? Mathf.Max(0, targetMonster.hp) : 0;
                return;
            }

            baseAmount = Mathf.Max(0, FormulaEvaluator.Evaluate(
                amountFormula,
                battleManager.battleContext,
                battleManager.playerData,
                (List<int>)null,
                formulaBaseValue));
            return;
        }
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

    private static bool IsTargetHpFormula(string formula)
    {
        return string.Equals(formula, "Target.Hp", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(formula, "TargetHp", System.StringComparison.OrdinalIgnoreCase);
    }

    private static Monster ResolveCurrentTarget(TrainingBattleManager battleManager)
    {
        if (battleManager.currentTarget != null && !battleManager.currentTarget.IsDead()) {
            return battleManager.currentTarget;
        }

        List<Monster> livingMonsters = battleManager.GetLivingMonsters();
        if (livingMonsters != null && livingMonsters.Count == 1) {
            return livingMonsters[0];
        }

        return null;
    }
}

/// <summary>
/// 효과 대상 구분값
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
    None,
    RandomEnemy
}
