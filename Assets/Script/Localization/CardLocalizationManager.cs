using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardLocalizationLanguage
{
    Korean,
    English,
    Japanese,
    ChineseTraditional,
    ChineseSimplified
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
}

public static class CardLocalizationManager
{
    private const string LocalizationResourcePath = "Localization/Cards";
    private const string LanguagePlayerPrefsKey = "CardLocalization.Language";

    private static bool isInitialized;
    private static CardLocalizationLanguage currentLanguage = CardLocalizationLanguage.Korean;
    private static readonly Dictionary<int, CardLocalizationEntry> localizedEntriesById = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        EnsureInitialized();
    }

    public static CardLocalizationLanguage GetCurrentLanguage()
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
        ApplyToAllCards();
    }

    public static void SetCurrentLanguage(CardLocalizationLanguage language)
    {
        EnsureInitialized();
        currentLanguage = language;
        PlayerPrefs.SetString(LanguagePlayerPrefsKey, ToLanguageCode(language));
        PlayerPrefs.Save();
        ApplyToAllCards();
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

    public static void ApplyToAllCards()
    {
        EnsureInitialized();
        if (!CardManager.IsInitialized())
        {
            return;
        }

        ApplyToCards(CardManager.GetAllCards());
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
            Debug.LogWarning("[CardLocalizationManager] 카드 로컬라이징 파일을 찾지 못했습니다");
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
                Debug.LogWarning($"[CardLocalizationManager] 로컬라이징 파일 파싱에 실패했습니다: {jsonFile.name}");
                continue;
            }

            if (table?.cards == null)
            {
                Debug.LogWarning($"[CardLocalizationManager] 로컬라이징 파일 형식이 올바르지 않습니다: {jsonFile.name}");
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
            if (TryParseLanguageCode(languageCode, out CardLocalizationLanguage savedLanguage))
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
        return ResolveLocalizedText(cardId, true, fallbackText);
    }

    private static string ResolveLocalizedCardDescription(int cardId, string fallbackText)
    {
        return ResolveLocalizedText(cardId, false, fallbackText);
    }

    private static string ResolveLocalizedText(int cardId, bool useName, string fallbackText)
    {
        if (!localizedEntriesById.TryGetValue(cardId, out CardLocalizationEntry entry) || entry == null)
        {
            return fallbackText ?? string.Empty;
        }

        string localizedText = GetLocalizedText(entry, currentLanguage, useName);
        if (!string.IsNullOrWhiteSpace(localizedText))
        {
            return localizedText;
        }

        string koreanText = GetLocalizedText(entry, CardLocalizationLanguage.Korean, useName);
        if (!string.IsNullOrWhiteSpace(koreanText))
        {
            return koreanText;
        }

        return fallbackText ?? string.Empty;
    }

    private static string GetLocalizedText(CardLocalizationEntry entry, CardLocalizationLanguage language, bool useName)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        return language switch
        {
            CardLocalizationLanguage.Korean => useName ? entry.koName : entry.koDescription,
            CardLocalizationLanguage.English => useName ? entry.enName : entry.enDescription,
            CardLocalizationLanguage.Japanese => useName ? entry.jaName : entry.jaDescription,
            CardLocalizationLanguage.ChineseTraditional => useName ? entry.zhHantName : entry.zhHantDescription,
            CardLocalizationLanguage.ChineseSimplified => useName ? entry.zhHansName : entry.zhHansDescription,
            _ => string.Empty
        };
    }

    private static CardLocalizationLanguage ResolveSystemLanguage(SystemLanguage systemLanguage)
    {
        return systemLanguage switch
        {
            SystemLanguage.English => CardLocalizationLanguage.English,
            SystemLanguage.Japanese => CardLocalizationLanguage.Japanese,
            SystemLanguage.ChineseTraditional => CardLocalizationLanguage.ChineseTraditional,
            SystemLanguage.ChineseSimplified => CardLocalizationLanguage.ChineseSimplified,
            _ => CardLocalizationLanguage.Korean
        };
    }

    private static string ToLanguageCode(CardLocalizationLanguage language)
    {
        return language switch
        {
            CardLocalizationLanguage.English => "en",
            CardLocalizationLanguage.Japanese => "ja",
            CardLocalizationLanguage.ChineseTraditional => "zh-Hant",
            CardLocalizationLanguage.ChineseSimplified => "zh-Hans",
            _ => "ko"
        };
    }

    private static bool TryParseLanguageCode(string languageCode, out CardLocalizationLanguage language)
    {
        language = CardLocalizationLanguage.Korean;
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        switch (languageCode.Trim())
        {
            case "ko":
                language = CardLocalizationLanguage.Korean;
                return true;

            case "en":
                language = CardLocalizationLanguage.English;
                return true;

            case "ja":
                language = CardLocalizationLanguage.Japanese;
                return true;

            case "zh-Hant":
                language = CardLocalizationLanguage.ChineseTraditional;
                return true;

            case "zh-Hans":
                language = CardLocalizationLanguage.ChineseSimplified;
                return true;

            default:
                return false;
        }
    }
}
