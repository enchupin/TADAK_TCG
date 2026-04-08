using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color unplayableColor = Color.gray;

    private bool isPlayable = true;
    private Card currentCard;
    public bool IsPlayable => isPlayable;

    private void Awake()
    {
        CacheTooltipReferences();
        HideBuffTooltip();
    }

    private void OnDisable()
    {
        HideBuffTooltip();
    }

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

        currentCard = card;
        CacheTooltipReferences();
        HideBuffTooltip();

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (costText != null)
            costText.text = card.HasKeyword(CardKeywordIds.Unplayable) ? "-" : card.cost.ToString();

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

    public void ShowBuffTooltip()
    {
        CacheTooltipReferences();

        if (tooltipPanel == null || tooltipText == null || currentCard == null || BuffManager.Instance == null)
        {
            HideBuffTooltip();
            return;
        }

        string buffTooltipText = BuildBuffTooltipText(currentCard);
        if (string.IsNullOrWhiteSpace(buffTooltipText))
        {
            HideBuffTooltip();
            return;
        }

        tooltipText.text = buffTooltipText;
        tooltipPanel.SetActive(true);
    }

    public void HideBuffTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
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
            CardKeywordIds.Keep => "보존",
            CardKeywordIds.Unplayable => "사용불가",
            CardKeywordIds.Exhaust => "소멸",
            CardKeywordIds.Power => "파워",
            CardKeywordIds.Opening => "개시",
            CardKeywordIds.Shadow => "그림자",
            CardKeywordIds.Finale => "종언",
            CardKeywordIds.Ghost => "유령",
            CardKeywordIds.Unique => "유일",
            _ => string.Empty
        };
    }

    private string BuildBuffTooltipText(Card card)
    {
        if (card == null || BuffManager.Instance == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        List<int> keywordIds = new();
        CollectKeywordIds(card.keywords, keywordIds);
        AppendKeywordTooltipText(builder, keywordIds);

        List<int> buffIds = new();
        CollectBuffIds(card.effects, buffIds);
        CollectBuffIds(card.keepEffects, buffIds);
        CollectBuffIds(card.endTurnInHandEffects, buffIds);
        AppendBuffTooltipText(builder, buffIds);

        return builder.ToString();
    }

    private void CollectKeywordIds(List<int> sourceIds, List<int> keywordIds)
    {
        if (sourceIds == null || keywordIds == null)
        {
            return;
        }

        foreach (int keywordId in sourceIds)
        {
            AddKeywordId(keywordIds, keywordId);
        }
    }

    private void CollectBuffIds(List<ICardEffect> effects, List<int> buffIds)
    {
        if (effects == null || buffIds == null)
        {
            return;
        }

        foreach (ICardEffect effect in effects)
        {
            CollectBuffIds(effect, buffIds);
        }
    }

    private void CollectBuffIds(ICardEffect effect, List<int> buffIds)
    {
        if (effect == null || buffIds == null)
        {
            return;
        }

        switch (effect)
        {
            case BuffEffect buffEffect:
                AddBuffId(buffIds, buffEffect.buffId);
                return;

            case RemoveBuffEffect removeBuffEffect:
                AddBuffId(buffIds, removeBuffEffect.buffId);
                return;

            case AttackEffect attackEffect:
                CollectBuffIds(attackEffect.onActions, buffIds);
                return;

            case DamageEffect damageEffect:
                CollectBuffIds(damageEffect.onActions, buffIds);
                return;

            case ChangeStatEffect changeStatEffect:
                CollectBuffIds(changeStatEffect.onActions, buffIds);
                return;

            case BarrierEffect barrierEffect:
                CollectBuffIds(barrierEffect.onActions, buffIds);
                return;

            case MoveEffect moveEffect:
                CollectBuffIds(moveEffect.onActions, buffIds);
                return;

            case ExhaustCardEffect exhaustCardEffect:
                CollectBuffIds(exhaustCardEffect.onActions, buffIds);
                return;

            case SelectCardEffect selectCardEffect:
                CollectBuffIds(selectCardEffect.onActions, buffIds);
                return;

            case ConditionalEffect conditionalEffect:
                CollectBuffIds(conditionalEffect.successEffects, buffIds);
                CollectBuffIds(conditionalEffect.failEffects, buffIds);
                return;

            case RepeatEffect repeatEffect:
                CollectBuffIds(repeatEffect.effectToRepeat, buffIds);
                return;
        }
    }

    private void AddBuffId(List<int> buffIds, int buffId)
    {
        if (buffIds == null || buffId <= 0 || buffIds.Contains(buffId) || BuffManager.Instance == null)
        {
            return;
        }

        if (!BuffManager.Instance.TryGetBuffData(buffId, out _))
        {
            return;
        }

        buffIds.Add(buffId);
    }

    private void AddKeywordId(List<int> keywordIds, int keywordId)
    {
        if (keywordIds == null || keywordId <= 0 || keywordIds.Contains(keywordId))
        {
            return;
        }

        keywordIds.Add(keywordId);
    }

    private void AppendKeywordTooltipText(StringBuilder builder, List<int> keywordIds)
    {
        if (builder == null || keywordIds == null)
        {
            return;
        }

        foreach (int keywordId in keywordIds)
        {
            string keywordName = GetKeywordDisplayName(keywordId);
            string keywordDescription = KeywordDatabase.GetKeywordDescription(keywordId);
            AppendTooltipEntry(builder, keywordName, keywordDescription);
        }
    }

    private void AppendBuffTooltipText(StringBuilder builder, List<int> buffIds)
    {
        if (builder == null || buffIds == null || BuffManager.Instance == null)
        {
            return;
        }

        foreach (int buffId in buffIds)
        {
            if (!BuffManager.Instance.TryGetBuffData(buffId, out BuffData buffData) || buffData == null)
            {
                continue;
            }

            AppendTooltipEntry(builder, buffData.name, buffData.description);
        }
    }

    private void AppendTooltipEntry(StringBuilder builder, string title, string description)
    {
        if (builder == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            builder.Append(title);
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                builder.AppendLine();
            }

            builder.Append(description);
        }
    }

    private void CacheTooltipReferences()
    {
        if (tooltipPanel == null)
        {
            Transform tooltipPanelTransform = FindChildTransform(transform, "TooltipPanel");
            if (tooltipPanelTransform != null)
            {
                tooltipPanel = tooltipPanelTransform.gameObject;
            }
        }

        if (tooltipText == null)
        {
            Transform tooltipTextTransform = FindChildTransform(transform, "TooltipText");
            if (tooltipTextTransform != null)
            {
                tooltipText = tooltipTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (tooltipText != null && cardNameText != null && tooltipText.font != cardNameText.font)
        {
            tooltipText.font = cardNameText.font;
            tooltipText.fontSharedMaterial = cardNameText.fontSharedMaterial;
        }
    }

    private Transform FindChildTransform(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform foundChild = FindChildTransform(child, childName);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
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

        if (!TryResolveAmount(card.effects, battleManager, card, out int amount))
        {
            return card.description.Replace("{amount}", "0");
        }

        return card.description.Replace("{amount}", amount.ToString());
    }

    private static bool TryResolveAmount(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard, out int amount)
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
                    amount = ResolveAttackAmount(attackEffect, battleManager, sourceCard);
                    return true;

                case DamageEffect damageEffect:
                    amount = ResolveDamageAmount(damageEffect, battleManager, sourceCard);
                    return true;

                case BarrierEffect barrierEffect:
                    amount = ResolveBarrierAmount(barrierEffect, battleManager);
                    return true;

                case ConditionalEffect conditionalEffect:
                    if (TryResolveConditionalAmount(conditionalEffect, battleManager, sourceCard, out amount))
                    {
                        return true;
                    }
                    break;

                case RepeatEffect repeatEffect:
                    if (TryResolveRepeatAmount(repeatEffect, battleManager, sourceCard, out amount))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static bool TryResolveConditionalAmount(ConditionalEffect conditionalEffect, TrainingBattleManager battleManager, Card sourceCard, out int amount)
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

        if (TryResolveAmount(primaryEffects, battleManager, sourceCard, out amount))
        {
            return true;
        }

        return TryResolveAmount(secondaryEffects, battleManager, sourceCard, out amount);
    }

    private static bool TryResolveRepeatAmount(RepeatEffect repeatEffect, TrainingBattleManager battleManager, Card sourceCard, out int amount)
    {
        amount = 0;
        if (repeatEffect?.effectToRepeat == null)
        {
            return false;
        }

        return TryResolveAmount(new List<ICardEffect> { repeatEffect.effectToRepeat }, battleManager, sourceCard, out amount);
    }

    private static int ResolveAttackAmount(AttackEffect effect, TrainingBattleManager battleManager, Card sourceCard)
    {
        int attackBoost = battleManager?.playerData != null
            ? battleManager.playerData.GetBuffStack(AttackBoostBuffId)
            : 0;

        ResolveAttackBaseAmount(effect, battleManager, out int baseAmount, out float cardMultiplier);
        baseAmount += Mathf.Max(0, attackBoost);
        baseAmount += Mathf.Max(0, battleManager != null ? battleManager.GetCardBaseDamageBonus(sourceCard, true) : 0);

        if (battleManager?.playerData == null)
        {
            int fallbackAmount = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            fallbackAmount = battleManager != null
                ? battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, fallbackAmount)
                : fallbackAmount;
            return ApplyPreviewTargetDamageMultiplier(fallbackAmount, effect?.target ?? TargetType.None, battleManager);
        }

        int resolvedAmount = battleManager.playerData.CalculateCardDamage(baseAmount, effect.ampMultiplier, cardMultiplier);
        resolvedAmount = battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, resolvedAmount);
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

    private static int ResolveDamageAmount(DamageEffect effect, TrainingBattleManager battleManager, Card sourceCard)
    {
        ResolveDamageBaseAmount(effect, battleManager, out int baseAmount, out float cardMultiplier);
        if (battleManager?.playerData == null)
        {
            int fallbackAmount = Mathf.Max(0, Mathf.FloorToInt(baseAmount * Mathf.Max(0f, cardMultiplier)));
            fallbackAmount = battleManager != null
                ? battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, fallbackAmount)
                : fallbackAmount;
            return ApplyPreviewTargetDamageMultiplier(fallbackAmount, effect?.target ?? TargetType.None, battleManager);
        }

        int resolvedAmount = battleManager.playerData.CalculateCardDamage(baseAmount, effect.ampMultiplier, cardMultiplier);
        resolvedAmount = battleManager.ApplyCardDamageRuntimeModifiers(sourceCard, resolvedAmount);
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
        bool hasEnhancedCorrosion = previewTarget.GetBuffStack(EnhancedCorrosionBuffId) > 0;
        bool hasCorrosion = previewTarget.GetBuffStack(CorrosionBuffId) > 0;
        bool playerEnhancesCorrosion = !hasEnhancedCorrosion
            && hasCorrosion
            && battleManager.playerData != null
            && battleManager.playerData.GetBuffStack(CorrosionEnhanceBuffId) > 0;

        if (hasEnhancedCorrosion || playerEnhancesCorrosion)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(EnhancedCorrosionBuffId, 1.5f)
                : 1.5f;
        }
        else if (hasCorrosion)
        {
            multiplier = BuffManager.Instance != null
                ? BuffManager.Instance.GetIncomingDamageMultiplier(CorrosionBuffId, 1.25f)
                : 1.25f;
        }

        return Mathf.Max(0, Mathf.FloorToInt(amount * multiplier));
    }
}
