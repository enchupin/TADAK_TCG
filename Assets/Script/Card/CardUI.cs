using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static BattleRuntimeDefinitions;

/// <summary>
/// Card UI component.
/// Displays card data and exposes playability visuals.
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI keywordText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image cardArtwork;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.gray;

    private bool isPlayable = true;
    public bool IsPlayable => isPlayable;

    /// <summary>
    /// Updates text/icon fields from card data.
    /// </summary>
    public void UpdateDisplay(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("[CardUI] Card is null!");
            return;
        }

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (costText != null)
            costText.text = card.cost.ToString();

        if (keywordText != null)
        {
            string keywordLine = BuildKeywordText(card);
            keywordText.text = keywordLine;
            keywordText.gameObject.SetActive(!string.IsNullOrEmpty(keywordLine));
        }

        if (descriptionText != null)
            descriptionText.text = CardDescriptionFormatter.Format(card, TrainingBattleManager.Instance);

        ApplyPlayableVisual();
    }

    public void SetPlayable(bool playable)
    {
        isPlayable = playable;
        ApplyPlayableVisual();
    }

    private void ApplyPlayableVisual()
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = isPlayable ? normalColor : unplayableColor;
    }

    private static string BuildKeywordText(Card card)
    {
        if (card?.keywords == null || card.keywords.Count == 0) {
            return string.Empty;
        }

        List<string> keywordNames = new();
        foreach (int keywordId in card.keywords)
        {
            string keywordName = GetKeywordDisplayName(keywordId);
            if (string.IsNullOrEmpty(keywordName) || keywordNames.Contains(keywordName)) {
                continue;
            }

            keywordNames.Add($"[{keywordName}]");
        }

        return string.Join(" ", keywordNames);
    }

    private static string GetKeywordDisplayName(int keywordId)
    {
        string keywordName = KeywordDatabase.GetKeywordName(keywordId);
        if (!string.IsNullOrWhiteSpace(keywordName))
        {
            return keywordName;
        }

        return keywordId switch
        {
            CardKeywordIds.Keep => "\uBCF4\uC874",
            CardKeywordIds.Unplayable => "\uC0AC\uC6A9\uBD88\uAC00",
            CardKeywordIds.Exhaust => "\uC18C\uBA78",
            CardKeywordIds.Power => "\uD30C\uC6CC",
            CardKeywordIds.Opening => "\uAC1C\uC2DC",
            CardKeywordIds.Shadow => "\uADF8\uB9BC\uC790",
            CardKeywordIds.Finale => "\uC885\uC5B8",
            CardKeywordIds.Ghost => "\uC720\uB839",
            CardKeywordIds.Unique => "\uC720\uC77C",
            _ => string.Empty
        };
    }
}

public static class CardDescriptionFormatter
{
    public static string Format(Card card, TrainingBattleManager battleManager)
    {
        if (card == null || string.IsNullOrEmpty(card.description))
        {
            return string.Empty;
        }

        if (card.description.IndexOf("{amount}", System.StringComparison.Ordinal) < 0)
        {
            return card.description;
        }

        if (!TryResolveAmount(card.effects, battleManager, out int amount))
        {
            return card.description.Replace("{amount}", "0");
        }

        return card.description.Replace("{amount}", amount.ToString());
    }

    private static bool TryResolveAmount(List<ICardEffect> effects, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effects == null || effects.Count == 0)
        {
            return false;
        }

        foreach (ICardEffect effect in effects)
        {
            if (effect == null)
            {
                continue;
            }

            switch (effect)
            {
                case AttackEffect attackEffect:
                    amount = ResolveAttackAmount(attackEffect, battleManager);
                    return true;

                case DamageEffect damageEffect:
                    amount = ResolveDamageAmount(damageEffect, battleManager);
                    return true;

                case BarrierEffect barrierEffect:
                    amount = ResolveBarrierAmount(barrierEffect, battleManager);
                    return true;

                case ConditionalEffect conditionalEffect:
                    if (TryResolveConditionalAmount(conditionalEffect, battleManager, out amount))
                    {
                        return true;
                    }
                    break;

                case RepeatEffect repeatEffect:
                    if (TryResolveRepeatAmount(repeatEffect, battleManager, out amount))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static bool TryResolveConditionalAmount(ConditionalEffect conditionalEffect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (conditionalEffect == null)
        {
            return false;
        }

        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
        List<ICardEffect> primaryEffects = conditionalEffect.failEffects;
        List<ICardEffect> secondaryEffects = conditionalEffect.successEffects;
        if (primaryEffects == null || primaryEffects.Count == 0)
        {
            primaryEffects = conditionalEffect.successEffects;
            secondaryEffects = conditionalEffect.failEffects;
        }

        if (TryResolveAmount(primaryEffects, battleManager, out amount))
        {
            return true;
        }

        return TryResolveAmount(secondaryEffects, battleManager, out amount);
    }

    private static bool TryResolveRepeatAmount(RepeatEffect repeatEffect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (repeatEffect?.effectToRepeat == null)
        {
            return false;
        }

        return TryResolveAmount(new List<ICardEffect> { repeatEffect.effectToRepeat }, battleManager, out amount);
    }

    private static int ResolveAttackAmount(AttackEffect effect, TrainingBattleManager battleManager)
    {
        int attackBoost = battleManager?.playerData != null
            ? battleManager.playerData.GetBuffStack(AttackBoostBuffId)
            : 0;

        ResolveAttackBaseAmount(effect, battleManager, out int baseAmount, out float cardMultiplier);
        baseAmount += Mathf.Max(0, attackBoost);

        if (battleManager?.playerData == null)
        {
            int fallbackAmount = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            return ApplyPreviewTargetDamageMultiplier(fallbackAmount, effect?.target ?? TargetType.None, battleManager);
        }

        int resolvedAmount = battleManager.playerData.CalculateCardDamage(baseAmount, effect.ampMultiplier, cardMultiplier);
        return ApplyPreviewTargetDamageMultiplier(resolvedAmount, effect?.target ?? TargetType.None, battleManager);
    }

    private static void ResolveAttackBaseAmount(AttackEffect effect, TrainingBattleManager battleManager, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = effect != null ? effect.amount : 0;

        if (effect == null || string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            return;
        }

        if (TryParseMultiplierFormula(effect.amountFormula, out float parsedMultiplier))
        {
            cardMultiplier = parsedMultiplier;
            return;
        }

        baseAmount = Mathf.Max(0, FormulaEvaluator.Evaluate(
            effect.amountFormula,
            battleManager?.battleContext,
            battleManager?.playerData,
            effect.cardIdList,
            effect.amount));
    }

    private static int ResolveDamageAmount(DamageEffect effect, TrainingBattleManager battleManager)
    {
        ResolveDamageBaseAmount(effect, battleManager, out int baseAmount, out float cardMultiplier);
        if (battleManager?.playerData == null)
        {
            int fallbackAmount = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            return ApplyPreviewTargetDamageMultiplier(fallbackAmount, effect?.target ?? TargetType.None, battleManager);
        }

        int resolvedAmount = battleManager.playerData.CalculateCardDamage(baseAmount, effect.ampMultiplier, cardMultiplier);
        return ApplyPreviewTargetDamageMultiplier(resolvedAmount, effect?.target ?? TargetType.None, battleManager);
    }

    private static void ResolveDamageBaseAmount(DamageEffect effect, TrainingBattleManager battleManager, out int baseAmount, out float cardMultiplier)
    {
        cardMultiplier = 1f;
        baseAmount = effect != null ? Mathf.Max(0, effect.amount) : 0;
        if (effect == null || string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            return;
        }

        if (TryParseMultiplierFormula(effect.amountFormula, out float parsedMultiplier))
        {
            cardMultiplier = parsedMultiplier;
            return;
        }

        if (IsTargetHpFormula(effect.amountFormula))
        {
            Monster targetMonster = ResolveCurrentTarget(battleManager);
            baseAmount = targetMonster != null ? Mathf.Max(0, targetMonster.hp) : 0;
            return;
        }

        baseAmount = Mathf.Max(0, FormulaEvaluator.Evaluate(
            effect.amountFormula,
            battleManager?.battleContext,
            battleManager?.playerData,
            null,
            effect.amount));
    }

    private static int ResolveBarrierAmount(BarrierEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        int resolvedAmount;
        if (!string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            resolvedAmount = Mathf.Max(0, FormulaEvaluator.Evaluate(
                effect.amountFormula,
                battleManager?.battleContext,
                battleManager?.playerData,
                0));
        }
        else if (effect.amount > 0)
        {
            resolvedAmount = effect.amount;
        }
        else
        {
            resolvedAmount = 0;
        }

        if (effect.target == TargetType.Self && battleManager != null)
        {
            resolvedAmount += Mathf.Max(0, battleManager.GetAdditionalBarrierGain());
        }

        return Mathf.Max(0, resolvedAmount);
    }

    private static bool TryParseMultiplierFormula(string formula, out float multiplier)
    {
        multiplier = 1f;
        if (string.IsNullOrWhiteSpace(formula))
        {
            return false;
        }

        string trimmed = formula.Trim();
        if (!trimmed.StartsWith("*"))
        {
            return false;
        }

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
        return battleManager?.GetDescriptionTarget();
    }

    private static int ApplyPreviewTargetDamageMultiplier(int amount, TargetType targetType, TrainingBattleManager battleManager)
    {
        if (amount <= 0 || targetType != TargetType.SingleEnemy || battleManager == null)
        {
            return Mathf.Max(0, amount);
        }

        Monster previewTarget = battleManager.GetDescriptionTarget(true);
        if (previewTarget == null)
        {
            return Mathf.Max(0, amount);
        }

        float multiplier = 1f;
        if (previewTarget.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(EnhancedCorrosionBuffId, 1.5f)
                : 1.5f;
        }
        else if (previewTarget.GetBuffStack(CorrosionBuffId) > 0)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(CorrosionBuffId, 1.25f)
                : 1.25f;
        }

        return Mathf.Max(0, Mathf.FloorToInt(amount * multiplier));
    }
}
