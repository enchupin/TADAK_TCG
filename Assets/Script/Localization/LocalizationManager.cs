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

[Serializable]
public class CardLocalizationTable
{
    public List<CardLocalizationEntry> cards = new();
}

[Serializable]
public class CardLocalizationEntry
{
    public int cardId;
    public string koName;
    public string koDescription;
    public string enName;
    public string enDescription;
    public string jaName;
    public string jaDescription;
    public string zhHantName;
    public string zhHantDescription;
    public string zhHansName;
    public string zhHansDescription;
    public string ruName;
    public string ruDescription;
}

public static class LocalizationManager
{
    private const string LocalizationResourcePath = "Localization/Cards";
    private const string LanguagePlayerPrefsKey = "Localization.Language";
    // 언어 목록과 언어별 문구 표 (Localization/<언어 코드>/*.json)
    private const string LanguageListResourcePath = "Localization/languages";
    private const string LanguageTableResourceRoot = "Localization/";
    private const string SourceLanguageCode = "ko";

    public static event Action LanguageChanged;

    private static bool isInitialized;
    private static LocalizationLanguage currentLanguage = LocalizationLanguage.Korean;
    private static string currentLanguageCode = SourceLanguageCode;
    private static LocalizationCatalog catalog;
    private static readonly HashSet<string> loadedLanguageTables = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> reportedMissingKeys = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, CardLocalizationEntry> localizedEntriesById = new();

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

    // 현재 언어 → 폴백 순서로 키를 찾음
    public static bool TryGetText(string key, out string text)
    {
        EnsureInitialized();
        foreach (string languageCode in catalog.GetFallbackChain(currentLanguageCode))
        {
            EnsureLanguageTableLoaded(languageCode);
        }

        return catalog.TryGetText(currentLanguageCode, key, out text);
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
        LocalizedTextFormatter.ReferenceResolver referenceResolver = null)
    {
        EnsureInitialized();
        return LocalizedTextFormatter.Format(template, currentLanguageCode, args, referenceResolver);
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
        ApplyToAllCards();
        ApplyToCurrentRunDeck();
        RefreshActiveCardControllers();
        LanguageChanged?.Invoke();
    }

    public static string ResolveLocalizedText(
        string koreanText,
        string englishText,
        string japaneseText,
        string chineseTraditionalText,
        string chineseSimplifiedText,
        string russianText,
        string fallbackText = "")
    {
        EnsureInitialized();
        return ResolveLocalizedText(
            currentLanguage,
            koreanText,
            englishText,
            japaneseText,
            chineseTraditionalText,
            chineseSimplifiedText,
            russianText,
            fallbackText);
    }

    public static string ResolveLocalizedText(
        LocalizationLanguage language,
        string koreanText,
        string englishText,
        string japaneseText,
        string chineseTraditionalText,
        string chineseSimplifiedText,
        string russianText,
        string fallbackText = "")
    {
        string localizedText = GetLocalizedText(
            language,
            koreanText,
            englishText,
            japaneseText,
            chineseTraditionalText,
            chineseSimplifiedText,
            russianText);

        if (!string.IsNullOrWhiteSpace(localizedText))
        {
            return localizedText;
        }

        if (language == LocalizationLanguage.Russian && !string.IsNullOrWhiteSpace(englishText))
        {
            return englishText;
        }

        if (!string.IsNullOrWhiteSpace(koreanText))
        {
            return koreanText;
        }

        return fallbackText ?? string.Empty;
    }

    public static bool TryGetLocalizedCardName(int cardId, out string localizedName)
    {
        EnsureInitialized();
        localizedName = ResolveLocalizedCardName(cardId, string.Empty);
        return !string.IsNullOrWhiteSpace(localizedName);
    }

    public static bool TryGetLocalizedCardDescription(int cardId, out string localizedDescription)
    {
        EnsureInitialized();
        localizedDescription = ResolveLocalizedCardDescription(cardId, string.Empty);
        return !string.IsNullOrWhiteSpace(localizedDescription);
    }

    public static void ApplyToCards(List<CardData> cards)
    {
        EnsureInitialized();
        if (cards == null)
        {
            return;
        }

        foreach (CardData card in cards)
        {
            ApplyToCard(card);
        }
    }

    public static void ApplyToRuntimeCard(Card card)
    {
        EnsureInitialized();
        if (card == null || card.cardId <= 0)
        {
            return;
        }

        card.cardName = ResolveLocalizedCardName(card.cardId, card.cardName);
        card.description = ResolveLocalizedCardDescription(card.cardId, card.description);
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

    public static void ApplyToAllCards()
    {
        EnsureInitialized();
        if (!CardManager.IsInitialized())
        {
            return;
        }

        ApplyToCards(CardManager.GetAllCards());
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
        LoadLocalizationTables();
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

    // 기존 6개 열 방식 데이터용: 목록에 없는 언어는 폴백 순서에서 처음 나오는 기존 언어로
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

    private static void LoadLocalizationTables()
    {
        localizedEntriesById.Clear();

        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(LocalizationResourcePath);
        if (jsonFiles == null || jsonFiles.Length == 0)
        {
            Debug.LogWarning("[LocalizationManager] 카드 로컬라이징 파일을 찾지 못했습니다");
            return;
        }

        foreach (TextAsset jsonFile in jsonFiles)
        {
            if (jsonFile == null)
            {
                continue;
            }

            CardLocalizationTable table;
            try
            {
                table = JsonUtility.FromJson<CardLocalizationTable>(jsonFile.text);
            }
            catch
            {
                Debug.LogWarning($"[LocalizationManager] 로컬라이징 파일 파싱에 실패했습니다: {jsonFile.name}");
                continue;
            }

            if (table?.cards == null)
            {
                Debug.LogWarning($"[LocalizationManager] 로컬라이징 파일 형식이 올바르지 않습니다: {jsonFile.name}");
                continue;
            }

            foreach (CardLocalizationEntry entry in table.cards)
            {
                if (entry == null || entry.cardId <= 0)
                {
                    continue;
                }

                localizedEntriesById[entry.cardId] = entry;
            }
        }
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

    private static void ApplyToCard(CardData card)
    {
        if (card == null || card.cardId <= 0)
        {
            return;
        }

        card.cardName = ResolveLocalizedCardName(card.cardId, card.cardName);
        card.description = ResolveLocalizedCardDescription(card.cardId, card.description);
    }

    private static string ResolveLocalizedCardName(int cardId, string fallbackText)
    {
        if (!localizedEntriesById.TryGetValue(cardId, out CardLocalizationEntry entry) || entry == null)
        {
            return fallbackText ?? string.Empty;
        }

        return ResolveLocalizedText(
            entry.koName,
            entry.enName,
            entry.jaName,
            entry.zhHantName,
            entry.zhHansName,
            entry.ruName,
            fallbackText);
    }

    private static string ResolveLocalizedCardDescription(int cardId, string fallbackText)
    {
        if (!localizedEntriesById.TryGetValue(cardId, out CardLocalizationEntry entry) || entry == null)
        {
            return fallbackText ?? string.Empty;
        }

        return ResolveLocalizedText(
            entry.koDescription,
            entry.enDescription,
            entry.jaDescription,
            entry.zhHantDescription,
            entry.zhHansDescription,
            entry.ruDescription,
            fallbackText);
    }

    private static string GetLocalizedText(
        LocalizationLanguage language,
        string koreanText,
        string englishText,
        string japaneseText,
        string chineseTraditionalText,
        string chineseSimplifiedText,
        string russianText)
    {
        return language switch
        {
            LocalizationLanguage.Korean => koreanText,
            LocalizationLanguage.English => englishText,
            LocalizationLanguage.Japanese => japaneseText,
            LocalizationLanguage.ChineseTraditional => chineseTraditionalText,
            LocalizationLanguage.ChineseSimplified => chineseSimplifiedText,
            LocalizationLanguage.Russian => russianText,
            _ => string.Empty
        };
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
