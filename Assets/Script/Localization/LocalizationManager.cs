using System;
using System.Collections.Generic;
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

    public static event Action LanguageChanged;

    private static bool isInitialized;
    private static LocalizationLanguage currentLanguage = LocalizationLanguage.Korean;
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
        return ToLanguageCode(currentLanguage);
    }

    public static void UseSystemLanguage()
    {
        EnsureInitialized();
        currentLanguage = ResolveSystemLanguage(Application.systemLanguage);
        PlayerPrefs.DeleteKey(LanguagePlayerPrefsKey);
        BuffMetadataDatabase.RefreshLocalizedText();
        ApplyToAllCards();
        ApplyToCurrentRunDeck();
        RefreshActiveCardControllers();
        LanguageChanged?.Invoke();
    }

    public static void SetCurrentLanguage(LocalizationLanguage language)
    {
        EnsureInitialized();
        currentLanguage = language;
        PlayerPrefs.SetString(LanguagePlayerPrefsKey, ToLanguageCode(language));
        PlayerPrefs.Save();
        BuffMetadataDatabase.RefreshLocalizedText();
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
        LoadLocalizationTables();
        LoadLanguagePreference();
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
            if (TryParseLanguageCode(languageCode, out LocalizationLanguage savedLanguage))
            {
                currentLanguage = savedLanguage;
                return;
            }
        }

        currentLanguage = ResolveSystemLanguage(Application.systemLanguage);
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

    private static LocalizationLanguage ResolveSystemLanguage(SystemLanguage systemLanguage)
    {
        return systemLanguage switch
        {
            SystemLanguage.English => LocalizationLanguage.English,
            SystemLanguage.Japanese => LocalizationLanguage.Japanese,
            SystemLanguage.ChineseTraditional => LocalizationLanguage.ChineseTraditional,
            SystemLanguage.ChineseSimplified => LocalizationLanguage.ChineseSimplified,
            SystemLanguage.Russian => LocalizationLanguage.Russian,
            _ => LocalizationLanguage.Korean
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
