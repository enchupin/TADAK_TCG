using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
    private const string CharacterBackgroundResourcePath = "Image/CardBase/CardBackGround/CardBackGround02_";
    private const string CharacterBarResourcePath = "Image/CardBase/CardBar/CardBar_";
    private const string CharacterCostResourcePath = "Image/CardBase/CardCost/Cost_";
    private static readonly Regex BuffTooltipPlaceholderPattern = new(@"\{(?<content>[^{}]+)\}", RegexOptions.Compiled);
    private static readonly Regex BuffTooltipMultipleWhitespacePattern = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly Regex BuffTooltipWhitespaceBeforePunctuationPattern = new(@"\s+([.,!?])", RegexOptions.Compiled);
    private static readonly Regex ReferencedBuffNamePattern = new(@"\((?<name>[^()]+)\)", RegexOptions.Compiled);
    private static readonly Dictionary<Character, Sprite> CharacterBackgroundCache = new();
    private static readonly Dictionary<Character, Sprite> CharacterBarCache = new();
    private static readonly Dictionary<Character, Sprite> CharacterCostCache = new();
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI keywordText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image barImage;
    [SerializeField] private Image costImage;
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
        CacheVisualReferences();
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
        CacheVisualReferences();
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

        ApplyCharacterBackground(card);
        ApplyPlayableVisual();
    }

    public void ShowBuffTooltip()
    {
        CacheTooltipReferences();

        if (tooltipPanel == null)
        {
            HideBuffTooltip();
            return;
        }

        if (tooltipText == null)
        {
            HideBuffTooltip();
            return;
        }

        if (currentCard == null)
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

    private void ApplyCharacterBackground(Card card)
    {
        if (card == null)
        {
            return;
        }

        CacheVisualReferences();

        if (backgroundImage != null)
        {
            backgroundImage.sprite = LoadCharacterBackground(card.character);
        }

        if (barImage != null)
        {
            barImage.sprite = LoadCharacterBar(card.character);
        }

        if (costImage != null)
        {
            costImage.sprite = LoadCharacterCost(card.character);
        }
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
        return KeywordDatabase.GetKeywordName(keywordId);
    }

    private string BuildBuffTooltipText(Card card)
    {
        if (card == null)
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
        if (buffIds == null || buffId <= 0 || buffIds.Contains(buffId))
        {
            return;
        }

        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out _))
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
        if (builder == null || buffIds == null)
        {
            return;
        }

        HashSet<int> appendedBuffIds = new();
        foreach (int buffId in buffIds)
        {
            AppendBuffTooltipEntryRecursive(builder, buffId, appendedBuffIds);
        }
    }

    private void AppendBuffTooltipEntryRecursive(StringBuilder builder, int buffId, HashSet<int> appendedBuffIds)
    {
        if (builder == null || appendedBuffIds == null || buffId <= 0 || appendedBuffIds.Contains(buffId))
        {
            return;
        }

        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData buffData) || buffData == null)
        {
            return;
        }

        appendedBuffIds.Add(buffId);
        AppendTooltipEntry(builder, buffData.name, FormatBuffTooltipDescription(buffData.description));

        List<int> referencedBuffIds = ExtractReferencedBuffIds(buffData.description);
        foreach (int referencedBuffId in referencedBuffIds)
        {
            AppendBuffTooltipEntryRecursive(builder, referencedBuffId, appendedBuffIds);
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

    private static string FormatBuffTooltipDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return CleanupCommonBuffTooltipText(BuffTooltipPlaceholderPattern.Replace(description, string.Empty));
    }

    private static string CleanupCommonBuffTooltipText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string cleaned = BuffTooltipMultipleWhitespacePattern.Replace(text, " ");
        cleaned = BuffTooltipWhitespaceBeforePunctuationPattern.Replace(cleaned, "$1");
        return cleaned.Trim();
    }

    private static List<int> ExtractReferencedBuffIds(string text)
    {
        List<int> referencedBuffIds = new();
        if (string.IsNullOrWhiteSpace(text))
        {
            return referencedBuffIds;
        }

        MatchCollection matches = ReferencedBuffNamePattern.Matches(text);
        foreach (Match match in matches)
        {
            string buffName = match.Groups["name"].Value.Trim();
            if (string.IsNullOrWhiteSpace(buffName))
            {
                continue;
            }

            if (!BuffMetadataDatabase.TryGetBuffId(buffName, out int buffId) || referencedBuffIds.Contains(buffId))
            {
                continue;
            }

            referencedBuffIds.Add(buffId);
        }

        return referencedBuffIds;
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
            Transform tooltipTextTransform = tooltipPanel != null
                ? FindChildTransform(tooltipPanel.transform, "TooltipText")
                : FindChildTransform(transform, "TooltipText");
            if (tooltipTextTransform != null)
            {
                tooltipText = tooltipTextTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (tooltipPanel == null)
        {
            Canvas[] tooltipCanvases = GetComponentsInChildren<Canvas>(true);
            foreach (Canvas candidateCanvas in tooltipCanvases)
            {
                if (candidateCanvas != null && candidateCanvas.gameObject.name == "TooltipPanel")
                {
                    tooltipPanel = candidateCanvas.gameObject;
                    break;
                }
            }
        }

        if (tooltipText == null)
        {
            TextMeshProUGUI[] tooltipTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI candidateText in tooltipTexts)
            {
                if (candidateText != null && candidateText.gameObject.name == "TooltipText")
                {
                    tooltipText = candidateText;
                    break;
                }
            }
        }

        if (tooltipText != null && cardNameText != null && tooltipText.font != cardNameText.font)
        {
            tooltipText.font = cardNameText.font;
            tooltipText.fontSharedMaterial = cardNameText.fontSharedMaterial;
        }
    }

    private void CacheVisualReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        if (barImage == null)
        {
            Transform barTransform = FindChildTransform(transform, "Bar");
            if (barTransform != null)
            {
                barImage = barTransform.GetComponent<Image>();
            }
        }

        if (costImage == null)
        {
            Transform costTransform = FindChildTransform(transform, "Cost");
            if (costTransform != null)
            {
                costImage = costTransform.GetComponent<Image>();
            }
        }
    }

    private static Sprite LoadCharacterBackground(Character character)
    {
        return LoadCharacterSprite(CharacterBackgroundCache, CharacterBackgroundResourcePath, character);
    }

    private static Sprite LoadCharacterBar(Character character)
    {
        return LoadCharacterSprite(CharacterBarCache, CharacterBarResourcePath, character);
    }

    private static Sprite LoadCharacterCost(Character character)
    {
        return LoadCharacterSprite(CharacterCostCache, CharacterCostResourcePath, character);
    }

    private static Sprite LoadCharacterSprite(Dictionary<Character, Sprite> cache, string resourcePath, Character character)
    {
        if (cache.TryGetValue(character, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string backgroundSuffix = GetCharacterBackgroundSuffix(character);
        Sprite loadedSprite = string.IsNullOrWhiteSpace(backgroundSuffix)
            ? null
            : Resources.Load<Sprite>(resourcePath + backgroundSuffix);

        cache[character] = loadedSprite;
        return loadedSprite;
    }

    private static string GetCharacterBackgroundSuffix(Character character)
    {
        return character.ToString();
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
    private static readonly Regex AmountPlaceholderPattern = new(@"\{amount(?<index>\d*)\}", RegexOptions.Compiled);
    private static readonly Regex ReferencedBuffNamePattern = new(@"\((?<name>[^()]+)\)", RegexOptions.Compiled);

    public static string Format(Card card, TrainingBattleManager battleManager)
    {
        if (card == null || string.IsNullOrEmpty(card.description))
        {
            return string.Empty;
        }

        if (card.description.IndexOf("{amount", System.StringComparison.Ordinal) < 0)
        {
            return RemoveReferencedBuffParentheses(card.description);
        }

        List<int> amounts = ResolveAmounts(card.effects, battleManager, card);
        if (amounts.Count == 0)
        {
            amounts.Add(0);
        }

        string formattedDescription = AmountPlaceholderPattern.Replace(card.description, match =>
        {
            string indexText = match.Groups["index"].Value;
            int resolvedIndex = 1;
            if (!string.IsNullOrEmpty(indexText) && (!int.TryParse(indexText, out resolvedIndex) || resolvedIndex <= 0))
            {
                resolvedIndex = 1;
            }

            int amountIndex = resolvedIndex - 1;
            if (amountIndex < 0 || amountIndex >= amounts.Count)
            {
                return "0";
            }

            return amounts[amountIndex].ToString();
        });

        return RemoveReferencedBuffParentheses(formattedDescription);
    }

    private static string RemoveReferencedBuffParentheses(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return ReferencedBuffNamePattern.Replace(description, "${name}");
    }

    private static List<int> ResolveAmounts(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard)
    {
        List<int> amounts = new List<int>();
        CollectResolvedAmounts(effects, battleManager, sourceCard, amounts);
        return amounts;
    }

    private static void CollectResolvedAmounts(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard, List<int> amounts)
    {
        if (effects == null || effects.Count == 0)
        {
            return;
        }

        foreach (ICardEffect effect in effects)
        {
            CollectResolvedAmount(effect, battleManager, sourceCard, amounts);
        }
    }

    private static void CollectResolvedAmount(ICardEffect effect, TrainingBattleManager battleManager, Card sourceCard, List<int> amounts)
    {
        if (effect == null)
        {
            return;
        }

        switch (effect)
        {
            case AttackEffect attackEffect:
                AddAmount(amounts, ResolveAttackAmount(attackEffect, battleManager, sourceCard));
                CollectOnActionAmounts(attackEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case DamageEffect damageEffect:
                AddAmount(amounts, ResolveDamageAmount(damageEffect, battleManager, sourceCard));
                CollectOnActionAmounts(damageEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case BarrierEffect barrierEffect:
                AddAmount(amounts, ResolveBarrierAmount(barrierEffect, battleManager));
                CollectOnActionAmounts(barrierEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case DrawEffect drawEffect:
                AddAmount(amounts, ResolveDrawAmount(drawEffect, battleManager));
                CollectOnActionAmounts(drawEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case DrawBasicEffect drawBasicEffect:
                AddAmount(amounts, ResolveDrawBasicAmount(drawBasicEffect, battleManager));
                CollectOnActionAmounts(drawBasicEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case DrawCharacterEffect drawCharacterEffect:
                AddAmount(amounts, ResolveDrawCharacterAmount(drawCharacterEffect, battleManager));
                CollectOnActionAmounts(drawCharacterEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case DrawUntilHandFullEffect drawUntilHandFullEffect:
                AddAmount(amounts, ResolveDrawUntilHandFullAmount(drawUntilHandFullEffect, battleManager));
                return;

            case HealEffect healEffect:
                AddAmount(amounts, ResolveHealAmount(healEffect, battleManager));
                return;

            case HpLossEffect hpLossEffect:
                AddAmount(amounts, ResolveHpLossAmount(hpLossEffect, battleManager));
                return;

            case StaminaEffect staminaEffect:
                AddAmount(amounts, ResolveStaminaAmount(staminaEffect, battleManager));
                return;

            case ScryEffect scryEffect:
                AddAmount(amounts, ResolveScryAmount(scryEffect));
                return;

            case BuffEffect buffEffect:
                AddAmount(amounts, ResolveBuffAmount(buffEffect, battleManager));
                return;

            case GenerateCardEffect generateCardEffect:
                AddAmount(amounts, ResolveGenerateCardAmount(generateCardEffect));
                return;

            case RandGenerateEffect randGenerateEffect:
                AddAmount(amounts, ResolveRandGenerateAmount(randGenerateEffect, battleManager));
                return;

            case TriggerEffect triggerEffect:
                AddAmount(amounts, ResolveTriggerAmount(triggerEffect, battleManager));
                return;

            case RemoveBuffEffect removeBuffEffect:
                AddAmount(amounts, ResolveRemoveBuffAmount(removeBuffEffect, battleManager));
                return;

            case ModifyCardEffect modifyCardEffect:
                if (TryResolveModifyCardAmount(modifyCardEffect, battleManager, out int modifyCardAmount))
                {
                    AddAmount(amounts, modifyCardAmount);
                }
                return;

            case SelectCardEffect selectCardEffect:
                AddAmount(amounts, Mathf.Max(0, selectCardEffect.count));
                CollectOnActionAmounts(selectCardEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case CopyEffect copyEffect:
                if (TryResolveCopyAmount(copyEffect, battleManager, out int copyAmount))
                {
                    AddAmount(amounts, copyAmount);
                }
                CollectOnActionAmounts(copyEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case ReduceCostEffect reduceCostEffect:
                AddAmount(amounts, Mathf.Abs(ResolveCardValueAmount(reduceCostEffect.amount, reduceCostEffect.amountFormula, battleManager, 0)));
                return;

            case CostEffect costEffect:
                AddAmount(amounts, Mathf.Abs(ResolveCardValueAmount(costEffect.amount, costEffect.amountFormula, battleManager, 0)));
                return;

            case ChangeStatEffect changeStatEffect:
                if (TryResolveChangeStatAmount(changeStatEffect, battleManager, out int changeStatAmount))
                {
                    AddAmount(amounts, changeStatAmount);
                }
                CollectOnActionAmounts(changeStatEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case MoveEffect moveEffect:
                if (TryResolveMoveAmount(moveEffect, battleManager, out int moveAmount))
                {
                    AddAmount(amounts, moveAmount);
                }
                CollectOnActionAmounts(moveEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case ExhaustCardEffect exhaustCardEffect:
                if (TryResolveExhaustAmount(exhaustCardEffect, battleManager, out int exhaustAmount))
                {
                    AddAmount(amounts, exhaustAmount);
                }
                CollectOnActionAmounts(exhaustCardEffect.onActions, battleManager, sourceCard, amounts);
                return;

            case ConditionalEffect conditionalEffect:
                CollectConditionalResolvedAmounts(conditionalEffect, battleManager, sourceCard, amounts);
                return;

            case RepeatEffect repeatEffect:
                if (TryResolveRepeatedDrawTotalAmount(repeatEffect, battleManager, out int repeatedDrawTotalAmount))
                {
                    AddAmount(amounts, repeatedDrawTotalAmount);
                }
                if (repeatEffect?.effectToRepeat != null)
                {
                    CollectResolvedAmount(repeatEffect.effectToRepeat, battleManager, sourceCard, amounts);
                }
                if (TryResolveRepeatCount(repeatEffect, battleManager, out int repeatCount))
                {
                    AddAmount(amounts, repeatCount);
                }
                return;
        }
    }

    private static void CollectOnActionAmounts(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard, List<int> amounts)
    {
        CollectResolvedAmounts(effects, battleManager, sourceCard, amounts);
    }

    private static void CollectConditionalResolvedAmounts(ConditionalEffect conditionalEffect, TrainingBattleManager battleManager, Card sourceCard, List<int> amounts)
    {
        if (conditionalEffect == null)
        {
            return;
        }

        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
        List<ICardEffect> primaryEffects = conditionalEffect.failEffects;
        List<ICardEffect> secondaryEffects = conditionalEffect.successEffects;
        if (primaryEffects == null || primaryEffects.Count == 0)
        {
            primaryEffects = conditionalEffect.successEffects;
            secondaryEffects = conditionalEffect.failEffects;
        }

        CollectResolvedAmounts(primaryEffects, battleManager, sourceCard, amounts);
        CollectResolvedAmounts(secondaryEffects, battleManager, sourceCard, amounts);
    }

    private static bool TryResolveAmount(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard, out int amount)
    {
        amount = 0;
        List<int> amounts = ResolveAmounts(effects, battleManager, sourceCard);
        if (amounts == null || amounts.Count == 0)
        {
            return false;
        }

        amount = amounts[0];
        return true;
    }

    private static bool TryResolveConditionalAmount(ConditionalEffect conditionalEffect, TrainingBattleManager battleManager, Card sourceCard, out int amount)
    {
        amount = 0;
        if (conditionalEffect == null)
        {
            return false;
        }

        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
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
        ResolveAttackBaseAmount(effect, battleManager, out int baseAmount, out float cardMultiplier);
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
            (List<int>)null,
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
            resolvedAmount = battleManager.ResolvePlayerBarrierGain(resolvedAmount);
        }

        return Mathf.Max(0, resolvedAmount);
    }

    private static int ResolveDrawAmount(DrawEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                effect.amountFormula,
                battleManager?.battleContext,
                battleManager?.playerData,
                effect.amount));
        }

        return Mathf.Max(0, effect.amount);
    }

    private static int ResolveDrawBasicAmount(DrawBasicEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                effect.amountFormula,
                battleManager?.battleContext,
                battleManager?.playerData,
                effect.amount));
        }

        return Mathf.Max(0, effect.amount);
    }

    private static int ResolveDrawCharacterAmount(DrawCharacterEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                effect.amountFormula,
                battleManager?.battleContext,
                battleManager?.playerData,
                effect.amount));
        }

        return Mathf.Max(0, effect.amount);
    }

    private static int ResolveHealAmount(HealEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static int ResolveHpLossAmount(HpLossEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static int ResolveDrawUntilHandFullAmount(DrawUntilHandFullEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static int ResolveStaminaAmount(StaminaEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static int ResolveScryAmount(ScryEffect effect)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, effect.count);
    }

    private static int ResolveBuffAmount(BuffEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        int resolvedAmount;
        if (!string.IsNullOrWhiteSpace(effect.amountFormula))
        {
            Monster formulaTargetMonster = CardEffectRuntimeUtility.ResolveSingleEnemyTarget(battleManager);
            resolvedAmount = FormulaEvaluator.Evaluate(
                effect.amountFormula,
                battleManager?.battleContext,
                battleManager?.playerData,
                formulaTargetMonster,
                effect.amount);
        }
        else
        {
            resolvedAmount = effect.amount;
        }

        if (effect.buffId > 0 && string.IsNullOrWhiteSpace(effect.amountFormula) && resolvedAmount <= 0)
        {
            resolvedAmount = 1;
        }

        return Mathf.Max(0, resolvedAmount);
    }

    private static int ResolveGenerateCardAmount(GenerateCardEffect effect)
    {
        if (effect == null)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(effect.cardId))
        {
            return 1;
        }

        if (effect.cardIdList != null && effect.cardIdList.Count > 0)
        {
            return 1;
        }

        if (effect.RandomCard != null && effect.RandomCard.Count > 0)
        {
            return 1;
        }

        return 0;
    }

    private static int ResolveRandGenerateAmount(RandGenerateEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static int ResolveTriggerAmount(TriggerEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 1));
    }

    private static int ResolveRemoveBuffAmount(RemoveBuffEffect effect, TrainingBattleManager battleManager)
    {
        if (effect == null)
        {
            return 0;
        }

        return Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
    }

    private static bool TryResolveModifyCardAmount(ModifyCardEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null)
        {
            return false;
        }

        amount = Mathf.Abs(ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveCopyAmount(CopyEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null)
        {
            return false;
        }

        if (string.Equals(effect.amountFormula, "all", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        amount = Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveChangeStatAmount(ChangeStatEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null)
        {
            return false;
        }

        if (string.Equals(effect.stat, "cardId", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        amount = Mathf.Abs(ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveMoveAmount(MoveEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null || string.Equals(effect.amountFormula, "all", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        amount = Mathf.Abs(ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveExhaustAmount(ExhaustCardEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null || string.Equals(effect.amountFormula, "all", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        amount = Mathf.Abs(ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveRepeatCount(RepeatEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect == null)
        {
            return false;
        }

        amount = Mathf.Max(0, ResolveCardValueAmount(effect.amount, effect.amountFormula, battleManager, 0));
        return amount > 0;
    }

    private static bool TryResolveRepeatedDrawTotalAmount(RepeatEffect effect, TrainingBattleManager battleManager, out int amount)
    {
        amount = 0;
        if (effect?.effectToRepeat == null || !TryResolveRepeatCount(effect, battleManager, out int repeatCount) || repeatCount <= 0)
        {
            return false;
        }

        int singleDrawAmount = effect.effectToRepeat switch
        {
            DrawEffect drawEffect => ResolveDrawAmount(drawEffect, battleManager),
            DrawBasicEffect drawBasicEffect => ResolveDrawBasicAmount(drawBasicEffect, battleManager),
            DrawCharacterEffect drawCharacterEffect => ResolveDrawCharacterAmount(drawCharacterEffect, battleManager),
            _ => 0
        };

        if (singleDrawAmount <= 0)
        {
            return false;
        }

        amount = singleDrawAmount * repeatCount;
        return amount > 0;
    }

    private static int ResolveCardValueAmount(int amount, string amountFormula, TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, battleManager?.battleContext, battleManager?.playerData, forwardedAmount);
        }

        if (amount != 0)
        {
            return amount;
        }

        return forwardedAmount;
    }

    private static void AddAmount(List<int> amounts, int amount)
    {
        if (amounts == null)
        {
            return;
        }

        amounts.Add(Mathf.Max(0, amount));
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

        return battleManager.ResolveMonsterIncomingDamage(previewTarget, amount);
    }
}
