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
    private const string CharacterBackgroundResourcePath = "Image/CardBase/CardBackGround/CardBackGround_";
    private const string CardArtworkResourcePath = "Image/CardBase/CardImage/";
    private static readonly Regex BuffTooltipPlaceholderPattern = new(@"\{(?<content>[^{}]+)\}", RegexOptions.Compiled);
    private static readonly Regex BuffTooltipMultipleWhitespacePattern = new(@"\s{2,}", RegexOptions.Compiled);
    private static readonly Regex BuffTooltipWhitespaceBeforePunctuationPattern = new(@"\s+([.,!?])", RegexOptions.Compiled);
    // 카드 툴팁에서 복수형을 고를 때 쓰는 일반 수량 (en other · ru many · fr other)
    private const int GenericPluralCount = 5;
    private static readonly Dictionary<string, object> GenericPluralArguments = new() { { "stack", GenericPluralCount } };
    private static readonly Dictionary<Character, Sprite> CharacterBackgroundCache = new();
    private static readonly Dictionary<int, Sprite> CardArtworkCache = new();
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
        ApplyCardArtwork(card);
        ApplyPlayableVisual();
    }

    // 카드 확인 패널의 효과 툴팁 위치 기준 (카드 안 툴팁 패널 자리)
    public RectTransform TooltipAnchor => tooltipPanel != null ? tooltipPanel.transform as RectTransform : null;

    /// <summary>
    /// 카드의 키워드 · 버프 효과 툴팁 문구를 효과마다 하나씩 반환 (전투 화면 버프 툴팁과 같은 서식)
    /// </summary>
    public List<string> BuildTooltipEntries()
    {
        List<string> entries = new();
        if (currentCard == null)
        {
            return entries;
        }

        TooltipColors colors = TooltipView.Colors;

        List<int> keywordIds = new();
        CollectKeywordIds(currentCard.keywords, keywordIds);
        AppendKeywordTooltipText(entries, keywordIds, colors);

        List<int> buffIds = new();
        CollectBuffIds(currentCard.effects, buffIds);
        CollectBuffIds(currentCard.keepEffects, buffIds);
        CollectBuffIds(currentCard.endTurnInHandEffects, buffIds);
        AppendBuffTooltipText(entries, buffIds, colors);

        return entries;
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

        if (backgroundImage != null)
        {
            backgroundImage.sprite = LoadCharacterBackground(card.character);
        }
    }

    private void ApplyCardArtwork(Card card)
    {
        if (cardArtwork == null)
        {
            return;
        }

        Sprite artworkSprite = card != null ? LoadCardArtwork(card.cardId) : null;
        cardArtwork.sprite = artworkSprite;
        cardArtwork.enabled = artworkSprite != null;
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

    private void AppendKeywordTooltipText(List<string> entries, List<int> keywordIds, TooltipColors colors)
    {
        if (entries == null || keywordIds == null)
        {
            return;
        }

        foreach (int keywordId in keywordIds)
        {
            string keywordName = GetKeywordDisplayName(keywordId);
            string keywordDescription = KeywordDatabase.GetKeywordDescription(keywordId);
            AppendTooltipEntry(entries, keywordName, keywordDescription, false, colors);
        }
    }

    private void AppendBuffTooltipText(List<string> entries, List<int> buffIds, TooltipColors colors)
    {
        if (entries == null || buffIds == null)
        {
            return;
        }

        HashSet<int> appendedBuffIds = new();
        foreach (int buffId in buffIds)
        {
            AppendBuffTooltipEntryRecursive(entries, buffId, appendedBuffIds, colors);
        }
    }

    private void AppendBuffTooltipEntryRecursive(List<string> entries, int buffId, HashSet<int> appendedBuffIds, TooltipColors colors)
    {
        if (entries == null || appendedBuffIds == null || buffId <= 0 || appendedBuffIds.Contains(buffId))
        {
            return;
        }

        if (!BuffMetadataDatabase.TryGetBuffData(buffId, out BuffData buffData) || buffData == null)
        {
            return;
        }

        appendedBuffIds.Add(buffId);
        AppendTooltipEntry(entries, buffData.name, FormatBuffTooltipDescription(buffData.description), BuffData.IsHarmfulBuffId(buffId), colors);

        List<int> referencedBuffIds = ExtractReferencedBuffIds(buffData.description);
        foreach (int referencedBuffId in referencedBuffIds)
        {
            AppendBuffTooltipEntryRecursive(entries, referencedBuffId, appendedBuffIds, colors);
        }
    }

    // 전투 버프 툴팁과 같은 서식: 극성 색 이름 한 줄 + 설명, 설명 안 (버프명) 참조는 그 버프의 극성 색
    private static void AppendTooltipEntry(List<string> entries, string title, string description, bool harmful, TooltipColors colors)
    {
        if (entries == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        entries.Add(BuffTooltipFormatter.FormatEntry(
            title,
            description,
            0,
            harmful,
            colors,
            BuffTooltipTextBuilder.TryResolveReference));
    }

    private static string FormatBuffTooltipDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // 카드 툴팁은 기존처럼 참조 버프를 (이름)으로 표시하고 중첩 수치 자리표시는 지움
        // 중첩 수치를 모르므로 복수형은 일반 복수(GenericPluralCount)로 고름
        string resolved = LocalizationManager.Format(description, GenericPluralArguments, GameText.ResolveParenthesizedReference, false);
        resolved = LocalizationManager.Format(resolved, RuneBuffArguments(), GameText.ResolveParenthesizedReference);
        return CleanupCommonBuffTooltipText(BuffTooltipPlaceholderPattern.Replace(resolved, string.Empty));
    }

    // 카드 툴팁에는 중첩 수치가 없음: 한국어는 기존처럼 수치를 지우고, 다른 언어는 문장이 깨지지 않게 X로 표시 (增加点 · на урона 방지)
    private static Dictionary<string, object> RuneBuffArguments()
    {
        string stackPlaceholder = LocalizationManager.GetCurrentLanguageCode() == "ko" ? string.Empty : "X";
        return new Dictionary<string, object>
        {
            { GameText.RuneBuffArgument, "(" + GameText.GetCurrentRuneBuffName() + ")" },
            { "stack", stackPlaceholder }
        };
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

    // 설명 틀의 [buff:id] 참조 (언어와 무관하게 ID로 추출)
    private static List<int> ExtractReferencedBuffIds(string text)
    {
        List<int> referencedBuffIds = new();
        if (string.IsNullOrWhiteSpace(text))
        {
            return referencedBuffIds;
        }

        foreach (LocalizedReference reference in LocalizedTextFormatter.ExtractReferences(text))
        {
            if (reference.Kind != LocalizedReference.Buff || referencedBuffIds.Contains(reference.Id))
            {
                continue;
            }

            if (BuffMetadataDatabase.TryGetBuffData(reference.Id, out _))
            {
                referencedBuffIds.Add(reference.Id);
            }
        }

        return referencedBuffIds;
    }

    private static Sprite LoadCharacterBackground(Character character)
    {
        return LoadCharacterSprite(CharacterBackgroundCache, CharacterBackgroundResourcePath, character);
    }

    private static Sprite LoadCardArtwork(int cardId)
    {
        int baseCardId = GetBaseCardArtworkId(cardId);
        if (baseCardId <= 0)
        {
            return null;
        }

        if (CardArtworkCache.TryGetValue(baseCardId, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(CardArtworkResourcePath + baseCardId.ToString(CultureInfo.InvariantCulture));
        CardArtworkCache[baseCardId] = loadedSprite;
        return loadedSprite;
    }

    private static int GetBaseCardArtworkId(int cardId)
    {
        return cardId/10;
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
}

public static class CardDescriptionFormatter
{
    private static readonly Regex AmountPlaceholderPattern = new(@"\{amount(?<index>\d*)\}", RegexOptions.Compiled);
    // {attack}, {attack2}는 공격·피해 효과 수치, {block}, {block2}는 보호막 효과 수치만 순서대로 가리킴
    private static readonly Regex KindPlaceholderPattern = new(@"\{(?<kind>attack|block)(?<index>\d*)\}", RegexOptions.Compiled);

    // 호기심 카드 설명 (ko: 강화됩니다.)
    private const string CuriosityDescriptionKey = "ui.card.curious";
    // 조각 줄 (ko: 조각: {description})
    private const string FragmentDescriptionKey = "ui.card.fragment";

    public static string Format(Card card, TrainingBattleManager battleManager)
    {
        if (card != null && card.isCurious)
        {
            return GameText.Ui(CuriosityDescriptionKey);
        }

        string fragmentDescription = BuildFragmentDescription(card);
        if (!string.IsNullOrEmpty(fragmentDescription))
        {
            return FormatBody(card, battleManager) + "\n" + fragmentDescription;
        }

        return FormatBody(card, battleManager);
    }

    // 조각이 새겨진 카드는 본래 설명 아래에 조각 효과를 덧붙임
    private static string BuildFragmentDescription(Card card)
    {
        if (card == null || !card.HasFragment || string.IsNullOrWhiteSpace(card.fragmentDescription))
        {
            return string.Empty;
        }

        return GameText.Ui(FragmentDescriptionKey, new Dictionary<string, object> { { "description", GameText.FormatPlain(card.fragmentDescription) } });
    }

    private static string FormatBody(Card card, TrainingBattleManager battleManager)
    {
        if (card == null || string.IsNullOrEmpty(card.description))
        {
            return string.Empty;
        }

        // 수치는 번역문의 복수형 인자로도 넘김 ({attack, plural, …})
        Dictionary<string, object> args = new();
        string description = ReplaceKindPlaceholders(card.description, card, battleManager, args);
        if (description.IndexOf("{amount", System.StringComparison.Ordinal) < 0)
        {
            return GameText.FormatPlain(description, args);
        }

        List<int> amounts = ResolveAmounts(card.effects, battleManager, card);
        if (amounts.Count == 0)
        {
            amounts.Add(0);
        }

        AddIndexedArguments(args, "amount", amounts);

        string formattedDescription = AmountPlaceholderPattern.Replace(description, match =>
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

        return GameText.FormatPlain(formattedDescription, args);
    }

    private static string ReplaceKindPlaceholders(string description, Card card, TrainingBattleManager battleManager, Dictionary<string, object> args)
    {
        if (string.IsNullOrEmpty(description)
            || (description.IndexOf("{attack", System.StringComparison.Ordinal) < 0 && description.IndexOf("{block", System.StringComparison.Ordinal) < 0))
        {
            return description;
        }

        List<int> attackAmounts = new List<int>();
        List<int> blockAmounts = new List<int>();
        CollectKindAmounts(card.effects, battleManager, card, attackAmounts, blockAmounts);
        AddIndexedArguments(args, "attack", attackAmounts);
        AddIndexedArguments(args, "block", blockAmounts);
        return KindPlaceholderPattern.Replace(description, match =>
        {
            List<int> source = match.Groups["kind"].Value == "attack" ? attackAmounts : blockAmounts;
            string indexText = match.Groups["index"].Value;
            int index = string.IsNullOrEmpty(indexText) ? 1 : int.TryParse(indexText, out int parsed) ? parsed : 1;
            return index >= 1 && index <= source.Count ? source[index - 1].ToString() : "0";
        });
    }

    private static void CollectKindAmounts(List<ICardEffect> effects, TrainingBattleManager battleManager, Card sourceCard, List<int> attackAmounts, List<int> blockAmounts)
    {
        if (effects == null)
        {
            return;
        }

        foreach (ICardEffect effect in effects)
        {
            switch (effect)
            {
                case AttackEffect attackEffect:
                    attackAmounts.Add(Mathf.Max(0, ResolveAttackAmount(attackEffect, battleManager, sourceCard)));
                    CollectKindAmounts(attackEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case DamageEffect damageEffect:
                    attackAmounts.Add(Mathf.Max(0, ResolveDamageAmount(damageEffect, battleManager, sourceCard)));
                    CollectKindAmounts(damageEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case BarrierEffect barrierEffect:
                    blockAmounts.Add(Mathf.Max(0, ResolveBarrierAmount(barrierEffect, battleManager)));
                    CollectKindAmounts(barrierEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case ConditionalEffect conditionalEffect:
                    // 기존 수치 수집과 같은 순서: 실패 분기(기본 효과)를 먼저
                    bool hasFailEffects = conditionalEffect.failEffects != null && conditionalEffect.failEffects.Count > 0;
                    CollectKindAmounts(hasFailEffects ? conditionalEffect.failEffects : conditionalEffect.successEffects, battleManager, sourceCard, attackAmounts, blockAmounts);
                    CollectKindAmounts(hasFailEffects ? conditionalEffect.successEffects : conditionalEffect.failEffects, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case RepeatEffect repeatEffect:
                    if (repeatEffect.effectToRepeat != null)
                    {
                        CollectKindAmounts(new List<ICardEffect> { repeatEffect.effectToRepeat }, battleManager, sourceCard, attackAmounts, blockAmounts);
                    }
                    break;
                case DrawEffect drawEffect:
                    CollectKindAmounts(drawEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case SelectCardEffect selectCardEffect:
                    CollectKindAmounts(selectCardEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case CopyEffect copyEffect:
                    CollectKindAmounts(copyEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case MoveEffect moveEffect:
                    CollectKindAmounts(moveEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
                case ExhaustCardEffect exhaustCardEffect:
                    CollectKindAmounts(exhaustCardEffect.onActions, battleManager, sourceCard, attackAmounts, blockAmounts);
                    break;
            }
        }
    }

    // attack = 첫 값, attack1 · attack2 … = 순서별 값
    private static void AddIndexedArguments(Dictionary<string, object> args, string name, List<int> values)
    {
        if (args == null || values == null || values.Count == 0)
        {
            return;
        }

        args[name] = values[0];
        for (int i = 0; i < values.Count; i++)
        {
            args[name + (i + 1)] = values[i];
        }
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

        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
        CollectResolvedAmounts(primaryEffects, battleManager, sourceCard, amounts);
        // 조건 분기 결과를 카드 설명 수치에 반영하지 않음
        CollectResolvedAmounts(secondaryEffects, battleManager, sourceCard, amounts);
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

        int resolvedAmount = battleManager.playerData.CalculateCardDamage(baseAmount, effect.ampMultiplier, cardMultiplier, false);
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
            resolvedAmount = battleManager.ResolvePlayerBarrierGain(resolvedAmount, true);
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
