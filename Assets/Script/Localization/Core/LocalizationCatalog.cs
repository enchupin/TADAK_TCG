using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// languages.json의 언어 한 개
/// </summary>
[Serializable]
public sealed class LocalizationLanguageInfo
{
    // 폴더 이름 겸 저장 값 (ko, en, zh-Hans …)
    public string code;
    // 설정 화면 표시 이름 (자기 언어로)
    public string nativeName;
    // UnityEngine.SystemLanguage 이름 (Korean, ChineseSimplified …)
    public List<string> systemLanguages = new();
    // 키가 없을 때 차례로 찾을 언어
    public List<string> fallback = new();
    // false면 설정 목록에 표시하지 않음 (번역 진행 중)
    public bool enabled = true;
}

/// <summary>
/// languages.json 최상위
/// </summary>
[Serializable]
public sealed class LocalizationLanguageList
{
    // 원문 언어, 모든 폴백의 마지막
    public string source = "ko";
    // 시스템 언어가 목록에 없을 때 사용할 언어
    public string defaultLanguage = "en";
    public List<LocalizationLanguageInfo> languages = new();
}

/// <summary>
/// 언어별 문구 표와 폴백 조회 (Unity 의존 없음)
/// </summary>
// 빈 문자열은 번역 누락으로 보고 폴백
public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, LocalizationLanguageInfo> languageByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LocalizationLanguageInfo> orderedLanguages = new();
    private readonly Dictionary<string, Dictionary<string, string>> entriesByLanguage = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> fallbackChainCache = new(StringComparer.OrdinalIgnoreCase);

    public string SourceLanguage { get; private set; } = "ko";
    public string DefaultLanguage { get; private set; } = "ko";

    public IReadOnlyList<LocalizationLanguageInfo> Languages => orderedLanguages;

    public LocalizationCatalog(LocalizationLanguageList languageList)
    {
        if (languageList == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(languageList.source))
        {
            SourceLanguage = languageList.source.Trim();
        }

        if (languageList.languages == null)
        {
            return;
        }

        foreach (LocalizationLanguageInfo language in languageList.languages)
        {
            if (language == null || string.IsNullOrWhiteSpace(language.code) || languageByCode.ContainsKey(language.code))
            {
                continue;
            }

            language.code = language.code.Trim();
            languageByCode.Add(language.code, language);
            orderedLanguages.Add(language);
        }

        LocalizationLanguageInfo defaultLanguage = FindLanguage(languageList.defaultLanguage?.Trim());
        DefaultLanguage = defaultLanguage != null && defaultLanguage.enabled ? defaultLanguage.code : SourceLanguage;
    }

    // 시스템 언어에 맞는 언어, 없으면 기본 언어
    public string ResolveSystemLanguage(string systemLanguageName)
    {
        return FindBySystemLanguage(systemLanguageName) ?? DefaultLanguage;
    }

    public bool HasLanguage(string languageCode)
    {
        return !string.IsNullOrEmpty(languageCode) && languageByCode.ContainsKey(languageCode);
    }

    public LocalizationLanguageInfo FindLanguage(string languageCode)
    {
        return !string.IsNullOrEmpty(languageCode) && languageByCode.TryGetValue(languageCode, out LocalizationLanguageInfo language)
            ? language
            : null;
    }

    // 등록된 표기로 정규화 (zh-hans → zh-Hans), 없으면 null
    public string NormalizeCode(string languageCode)
    {
        return FindLanguage(languageCode?.Trim())?.code;
    }

    // 시스템 언어 이름에 맞는 언어 코드 (사용 가능한 언어만), 없으면 null
    public string FindBySystemLanguage(string systemLanguageName)
    {
        if (string.IsNullOrWhiteSpace(systemLanguageName))
        {
            return null;
        }

        foreach (LocalizationLanguageInfo language in orderedLanguages)
        {
            if (!language.enabled || language.systemLanguages == null)
            {
                continue;
            }

            foreach (string systemLanguage in language.systemLanguages)
            {
                if (string.Equals(systemLanguage, systemLanguageName, StringComparison.OrdinalIgnoreCase))
                {
                    return language.code;
                }
            }
        }

        return null;
    }

    // 같은 키가 이미 있으면 덮어씀
    public void AddEntries(string languageCode, IEnumerable<KeyValuePair<string, string>> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || entries == null)
        {
            return;
        }

        string code = NormalizeCode(languageCode) ?? languageCode.Trim();
        if (!entriesByLanguage.TryGetValue(code, out Dictionary<string, string> table))
        {
            table = new Dictionary<string, string>(StringComparer.Ordinal);
            entriesByLanguage.Add(code, table);
        }

        foreach (KeyValuePair<string, string> entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Key))
            {
                continue;
            }

            table[entry.Key.Trim()] = entry.Value ?? string.Empty;
        }
    }

    public void ClearEntries(string languageCode)
    {
        if (!string.IsNullOrEmpty(languageCode))
        {
            entriesByLanguage.Remove(NormalizeCode(languageCode) ?? languageCode);
        }
    }

    public bool HasEntries(string languageCode)
    {
        return !string.IsNullOrEmpty(languageCode)
            && entriesByLanguage.TryGetValue(NormalizeCode(languageCode) ?? languageCode, out Dictionary<string, string> table)
            && table.Count > 0;
    }

    // 해당 언어 표에 직접 있는 문구만 (폴백 없음)
    public bool TryGetOwnText(string languageCode, string key, out string text)
    {
        text = null;
        if (string.IsNullOrEmpty(languageCode) || string.IsNullOrEmpty(key))
        {
            return false;
        }

        return entriesByLanguage.TryGetValue(NormalizeCode(languageCode) ?? languageCode, out Dictionary<string, string> table)
            && table.TryGetValue(key, out text)
            && !string.IsNullOrEmpty(text);
    }

    // 폴백 순서대로 찾음, 찾은 언어도 돌려줌
    public bool TryGetText(string languageCode, string key, out string text, out string foundLanguage)
    {
        foreach (string code in GetFallbackChain(languageCode))
        {
            if (TryGetOwnText(code, key, out text))
            {
                foundLanguage = code;
                return true;
            }
        }

        text = null;
        foundLanguage = null;
        return false;
    }

    public bool TryGetText(string languageCode, string key, out string text)
    {
        return TryGetText(languageCode, key, out text, out _);
    }

    public IEnumerable<string> GetKeys(string languageCode)
    {
        return !string.IsNullOrEmpty(languageCode) && entriesByLanguage.TryGetValue(NormalizeCode(languageCode) ?? languageCode, out Dictionary<string, string> table)
            ? table.Keys
            : Array.Empty<string>();
    }

    // 요청 언어 → languages.json의 fallback → 원문 언어 (중복 · 순환 제거)
    public IReadOnlyList<string> GetFallbackChain(string languageCode)
    {
        string code = NormalizeCode(languageCode) ?? languageCode?.Trim() ?? SourceLanguage;
        if (fallbackChainCache.TryGetValue(code, out List<string> cached))
        {
            return cached;
        }

        List<string> chain = new();
        AppendChain(code, chain);
        if (!chain.Contains(SourceLanguage, StringComparer.OrdinalIgnoreCase))
        {
            chain.Add(SourceLanguage);
        }

        fallbackChainCache[code] = chain;
        return chain;
    }

    private void AppendChain(string code, List<string> chain)
    {
        if (string.IsNullOrEmpty(code) || chain.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        chain.Add(code);
        LocalizationLanguageInfo language = FindLanguage(code);
        if (language?.fallback == null)
        {
            return;
        }

        foreach (string fallbackCode in language.fallback)
        {
            AppendChain(NormalizeCode(fallbackCode) ?? fallbackCode, chain);
        }
    }
}
