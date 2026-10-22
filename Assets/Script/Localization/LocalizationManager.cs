using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public enum LocalizationLanguage
{
    Korean,
    English,
    Japanese,
    ChineseTraditional,
    ChineseSimplified,
    Russian
}

public static class LocalizationManager
{
    private const string LanguagePlayerPrefsKey = "Localization.Language";
    // 언어 목록과 언어별 문구 표 (Localization/<언어 코드>/*.json)
    private const string LanguageListResourcePath = "Localization/languages";
    private const string LanguageTableResourceRoot = "Localization/";
    private const string SourceLanguageCode = "ko";
    // 의사 언어의 원본 (라틴 문자 변환 대상)
    private const string PseudoSourceLanguage = "en";

    public static event Action LanguageChanged;

    private static bool isInitialized;
    private static LocalizationLanguage currentLanguage = LocalizationLanguage.Korean;
    private static string currentLanguageCode = SourceLanguageCode;
    private static LocalizationCatalog catalog;
    private static readonly HashSet<string> loadedLanguageTables = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> reportedMissingKeys = new(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureInitialized();
    }

    public static LocalizationLanguage GetCurrentLanguage()
    {
        EnsureInitialized();
        return currentLanguage;
    }

    public static string GetCurrentLanguageCode()
    {
        EnsureInitialized();
        return currentLanguageCode;
    }

    // 저장된 언어 없이 시스템 언어를 따르는 중인지 (설정 화면 표시용)
    public static bool IsFollowingSystemLanguage()
    {
        EnsureInitialized();
        return !PlayerPrefs.HasKey(LanguagePlayerPrefsKey);
    }

    // 현재 언어 → languages.json 폴백 → 원문 언어 순서
    public static IReadOnlyList<string> GetFallbackChain()
    {
        EnsureInitialized();
        return catalog.GetFallbackChain(currentLanguageCode);
    }

    // 설정 화면 목록용 (enabled: false 언어는 기본 제외)
    public static IReadOnlyList<LocalizationLanguageInfo> GetLanguages(bool includeDisabled = false)
    {
        EnsureInitialized();
        List<LocalizationLanguageInfo> languages = new();
        foreach (LocalizationLanguageInfo language in catalog.Languages)
        {
            if (includeDisabled || language.enabled)
            {
                languages.Add(language);
            }
        }

        return languages;
    }

    public static void UseSystemLanguage()
    {
        EnsureInitialized();
        ApplyLanguageCode(catalog.ResolveSystemLanguage(Application.systemLanguage.ToString()));
        PlayerPrefs.DeleteKey(LanguagePlayerPrefsKey);
        NotifyLanguageChanged();
    }

    public static void SetCurrentLanguage(LocalizationLanguage language)
    {
        SetCurrentLanguage(ToLanguageCode(language));
    }

    // languages.json에 있는 코드만 적용
    public static bool SetCurrentLanguage(string languageCode)
    {
        EnsureInitialized();
        string normalizedCode = catalog.NormalizeCode(languageCode);
        if (normalizedCode == null)
        {
            Debug.LogWarning($"[LocalizationManager] 등록되지 않은 언어 코드입니다: {languageCode}");
            return false;
        }

        ApplyLanguageCode(normalizedCode);
        PlayerPrefs.SetString(LanguagePlayerPrefsKey, normalizedCode);
        PlayerPrefs.Save();
        NotifyLanguageChanged();
        return true;
    }

    // 개발용 의사 언어 (영어 문구를 변환해 표시, 하드코딩 · 칸 넘침 확인)
    public static bool PseudoLocalizationEnabled { get; private set; }

    public static void SetPseudoLocalization(bool enabled)
    {
        EnsureInitialized();
        if (PseudoLocalizationEnabled == enabled)
        {
            return;
        }

        PseudoLocalizationEnabled = enabled;
        NotifyLanguageChanged();
    }

    // 현재 언어 → 폴백 순서로 키를 찾음
    public static bool TryGetText(string key, out string text)
    {
        EnsureInitialized();
        string languageCode = PseudoLocalizationEnabled ? PseudoSourceLanguage : currentLanguageCode;
        foreach (string code in catalog.GetFallbackChain(languageCode))
        {
            EnsureLanguageTableLoaded(code);
        }

        bool found = catalog.TryGetText(languageCode, key, out text);
        if (found && PseudoLocalizationEnabled)
        {
            text = PseudoLocalizer.Transform(text);
        }

        return found;
    }

    // 키 조회 + 인자 · 복수형 · 참조 해석, 키가 없으면 키 그대로
    public static string GetText(
        string key,
        IReadOnlyDictionary<string, object> args = null,
        LocalizedTextFormatter.ReferenceResolver referenceResolver = null)
    {
        if (!TryGetText(key, out string template))
        {
            ReportMissingKey(key);
            return key ?? string.Empty;
        }

        return Format(template, args, referenceResolver);
    }

    // 표에서 가져온 문구를 현재 언어 규칙으로 해석
    public static string Format(
        string template,
        IReadOnlyDictionary<string, object> args = null,
        LocalizedTextFormatter.ReferenceResolver referenceResolver = null,
        bool substituteSimpleArguments = true)
    {
        EnsureInitialized();
        string languageCode = PseudoLocalizationEnabled ? PseudoSourceLanguage : currentLanguageCode;
        return LocalizedTextFormatter.Format(template, languageCode, args, referenceResolver, substituteSimpleArguments);
    }

    private static void ApplyLanguageCode(string languageCode)
    {
        currentLanguageCode = catalog.NormalizeCode(languageCode) ?? catalog.SourceLanguage;
        currentLanguage = ResolveLegacyLanguage(currentLanguageCode);
        foreach (string code in catalog.GetFallbackChain(currentLanguageCode))
        {
            EnsureLanguageTableLoaded(code);
        }
    }

    private static void NotifyLanguageChanged()
    {
        BuffMetadataDatabase.RefreshLocalizedText();
        CharacterManager.RefreshLocalizedText();
        ApplyToCurrentRunDeck();
        RefreshActiveCardControllers();
        LanguageChanged?.Invoke();
    }

    // 런타임 카드 이름 · 설명 틀을 현재 언어로 (CardData 원본은 한국어 그대로 둠)
    public static void ApplyToRuntimeCard(Card card)
    {
        EnsureInitialized();
        if (card == null || card.cardId <= 0)
        {
            return;
        }

        CardData sourceCard = CardManager.GetCard(card.cardId);
        card.cardName = GameText.CardName(card.cardId, sourceCard != null ? sourceCard.cardName : card.cardName);
        card.description = GameText.CardDescription(card.cardId, sourceCard != null ? sourceCard.description : card.description);
        // 새긴 조각 문구는 원래 카드 설명에서 다시 뽑음
        if (card.fragmentSourceCardId > 0)
        {
            card.fragmentDescription = FragmentEffect.ExtractQuotedText(GameText.CardDescription(card.fragmentSourceCardId));
        }
    }

    public static void ApplyToRuntimeCards(IEnumerable<Card> cards)
    {
        EnsureInitialized();
        if (cards == null)
        {
            return;
        }

        foreach (Card card in cards)
        {
            ApplyToRuntimeCard(card);
        }
    }

    private static void ApplyToCurrentRunDeck()
    {
        if (TrainingBattleManager.buildingDeck == null)
        {
            return;
        }

        TrainingBattleManager.buildingDeck.RefreshLocalizedTexts();
    }

    private static void RefreshActiveCardControllers()
    {
        CardController[] controllers = UnityEngine.Object.FindObjectsByType<CardController>(FindObjectsInactive.Exclude);
        foreach (CardController controller in controllers)
        {
            if (controller == null || controller.Card == null || controller.cardUI == null)
            {
                continue;
            }

            ApplyToRuntimeCard(controller.Card);
            controller.cardUI.UpdateDisplay(controller.Card);
        }
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        LoadLanguageList();
        LoadLanguagePreference();
    }

    private static void LoadLanguageList()
    {
        LocalizationLanguageList languageList = null;
        TextAsset jsonFile = Resources.Load<TextAsset>(LanguageListResourcePath);
        if (jsonFile != null)
        {
            try
            {
                languageList = JsonConvert.DeserializeObject<LocalizationLanguageList>(jsonFile.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LocalizationManager] languages.json 파싱에 실패했습니다: {exception.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[LocalizationManager] languages.json을 찾지 못해 기본 언어 목록을 사용합니다");
        }

        catalog = new LocalizationCatalog(languageList != null && languageList.languages.Count > 0
            ? languageList
            : CreateDefaultLanguageList());
        loadedLanguageTables.Clear();
        reportedMissingKeys.Clear();
    }

    // languages.json이 없을 때의 기존 6개 언어
    private static LocalizationLanguageList CreateDefaultLanguageList()
    {
        LocalizationLanguageList languageList = new() { source = SourceLanguageCode, defaultLanguage = SourceLanguageCode };
        foreach (LocalizationLanguage language in Enum.GetValues(typeof(LocalizationLanguage)))
        {
            languageList.languages.Add(new LocalizationLanguageInfo
            {
                code = ToLanguageCode(language),
                nativeName = language.ToString(),
                systemLanguages = new List<string> { language.ToString() }
            });
        }

        return languageList;
    }

    // Localization/<코드>/ 아래 json을 모두 읽어 한 표로 합침
    private static void EnsureLanguageTableLoaded(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !loadedLanguageTables.Add(languageCode))
        {
            return;
        }

        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(LanguageTableResourceRoot + languageCode);
        if (jsonFiles == null)
        {
            return;
        }

        foreach (TextAsset jsonFile in jsonFiles)
        {
            if (jsonFile == null)
            {
                continue;
            }

            try
            {
                Dictionary<string, string> entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonFile.text);
                catalog.AddEntries(languageCode, entries);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LocalizationManager] 문구 표 파싱에 실패했습니다: {languageCode}/{jsonFile.name} ({exception.Message})");
            }
        }
    }

    // 누락 키는 개발 빌드에서 키마다 한 번만 경고
    private static void ReportMissingKey(string key)
    {
        if (!Debug.isDebugBuild || string.IsNullOrEmpty(key) || !reportedMissingKeys.Add(key))
        {
            return;
        }

        Debug.LogWarning($"[LocalizationManager] 문구 키가 없습니다: {key} ({currentLanguageCode})");
    }

    // 기존 enum 사용처용: 목록에 없는 언어는 폴백 순서에서 처음 나오는 기존 언어로
    private static LocalizationLanguage ResolveLegacyLanguage(string languageCode)
    {
        foreach (string code in catalog.GetFallbackChain(languageCode))
        {
            if (TryParseLanguageCode(code, out LocalizationLanguage language))
            {
                return language;
            }
        }

        return LocalizationLanguage.Korean;
    }

    private static void LoadLanguagePreference()
    {
        if (PlayerPrefs.HasKey(LanguagePlayerPrefsKey))
        {
            string languageCode = PlayerPrefs.GetString(LanguagePlayerPrefsKey);
            // 저장 값은 기존과 같은 코드 (ko, en, zh-Hant …)
            string savedLanguageCode = catalog.NormalizeCode(languageCode);
            if (savedLanguageCode != null)
            {
                ApplyLanguageCode(savedLanguageCode);
                return;
            }
        }

        ApplyLanguageCode(catalog.ResolveSystemLanguage(Application.systemLanguage.ToString()));
    }

    private static string ToLanguageCode(LocalizationLanguage language)
    {
        return language switch
        {
            LocalizationLanguage.English => "en",
            LocalizationLanguage.Japanese => "ja",
            LocalizationLanguage.ChineseTraditional => "zh-Hant",
            LocalizationLanguage.ChineseSimplified => "zh-Hans",
            LocalizationLanguage.Russian => "ru",
            _ => "ko"
        };
    }

    private static bool TryParseLanguageCode(string languageCode, out LocalizationLanguage language)
    {
        language = LocalizationLanguage.Korean;
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        switch (languageCode.Trim())
        {
            case "ko":
                language = LocalizationLanguage.Korean;
                return true;

            case "en":
                language = LocalizationLanguage.English;
                return true;

            case "ja":
                language = LocalizationLanguage.Japanese;
                return true;

            case "zh-Hant":
                language = LocalizationLanguage.ChineseTraditional;
                return true;

            case "zh-Hans":
                language = LocalizationLanguage.ChineseSimplified;
                return true;

            case "ru":
                language = LocalizationLanguage.Russian;
                return true;

            default:
                return false;
        }
    }
}
