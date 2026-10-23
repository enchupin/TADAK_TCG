using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 번역 표 검증 (UnityEditor 의존 없음)
/// </summary>
// 원문(ko)과 비교: 누락 · 원문에 없는 키 · 인자 · 참조 불일치 · 복수형 형태 · 남은 한글 · 조각 따옴표 · 렌더링 후 남은 토큰
public static class LocalizationValidator
{
    // 한국어 단위가 붙은 자리표시 ({stack장} · {stack회})는 다른 언어에서 {stack} + 복수형으로 씀
    private static readonly Regex KoreanUnitPlaceholder = new(@"\{stack(장|회)\}", RegexOptions.Compiled);
    private static readonly Regex PluralBlockPattern = new(@"\{\s*(?<name>[A-Za-z0-9_]+)\s*,\s*plural\s*,", RegexOptions.Compiled);
    // 조각 카드: 코드가 큰따옴표 · “ ” · 「 」 안의 문구를 잘라 씀 (FragmentEffect.ExtractQuotedText)
    private static readonly char[] FragmentQuoteOpeners = { '"', '“', '「' };

    public static List<LocalizationIssue> Validate(
        IReadOnlyDictionary<string, string> source,
        IReadOnlyDictionary<string, string> target,
        string languageCode)
    {
        List<LocalizationIssue> issues = new();
        foreach (KeyValuePair<string, string> entry in source)
        {
            if (string.IsNullOrEmpty(entry.Value))
            {
                continue;
            }

            if (!target.TryGetValue(entry.Key, out string translated) || string.IsNullOrEmpty(translated))
            {
                issues.Add(new LocalizationIssue(languageCode, entry.Key, LocalizationIssueKind.Missing, "번역 없음"));
                continue;
            }

            ValidateEntry(entry.Key, entry.Value, translated, languageCode, issues);
        }

        foreach (string key in target.Keys)
        {
            if (!source.ContainsKey(key))
            {
                issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Orphan, "원문에 없는 키"));
            }
        }

        return issues;
    }

    public static void ValidateEntry(string key, string sourceText, string translated, string languageCode, List<LocalizationIssue> issues)
    {
        HashSet<string> sourceArgs = LocalizedTextFormatter.ExtractArgumentNames(KoreanUnitPlaceholder.Replace(sourceText, "{stack}"));
        HashSet<string> targetArgs = LocalizedTextFormatter.ExtractArgumentNames(translated);
        if (!sourceArgs.SetEquals(targetArgs))
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Arguments,
                $"인자 불일치: 원문 {{{string.Join(", ", sourceArgs.OrderBy(a => a))}}} / 번역 {{{string.Join(", ", targetArgs.OrderBy(a => a))}}}"));
        }

        HashSet<(string, int)> sourceRefs = ReferenceSet(sourceText);
        HashSet<(string, int)> targetRefs = ReferenceSet(translated);
        if (!sourceRefs.SetEquals(targetRefs))
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.References,
                $"참조 불일치: 원문 {FormatRefs(sourceRefs)} / 번역 {FormatRefs(targetRefs)}"));
        }

        if (languageCode != LocalizationSourceSync.SourceLanguage && ContainsHangul(StripTokens(translated)))
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Untranslated, "한글이 남아 있음"));
        }

        if (sourceText.IndexOf('"') >= 0 && translated.IndexOfAny(FragmentQuoteOpeners) < 0)
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Quote, "조각 문구 따옴표(\" “ 「)가 없음"));
        }

        foreach (string missing in MissingPluralCategories(translated, languageCode))
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Plural, missing));
        }

        string rendered = Render(translated, languageCode, sourceArgs.Union(targetArgs));
        if (rendered.IndexOf('{') >= 0 || rendered.IndexOf('}') >= 0 || rendered.Contains("[buff:") || rendered.Contains("[keyword:") || rendered.Contains("[card:"))
        {
            issues.Add(new LocalizationIssue(languageCode, key, LocalizationIssueKind.Render, $"해석 후 토큰이 남음: {rendered}"));
        }
    }

    // 모든 인자에 예시 값을 넣고 참조를 이름으로 바꾼 결과
    public static string Render(string text, string languageCode, IEnumerable<string> argumentNames)
    {
        Dictionary<string, object> args = new(StringComparer.Ordinal);
        foreach (string name in argumentNames)
        {
            args[name] = 3;
        }

        static bool Resolve(LocalizedReference reference, out string resolved)
        {
            resolved = reference.HasDisplayOverride ? reference.DisplayOverride : "NAME";
            return true;
        }

        // 의도 문장처럼 인자로 참조를 만드는 틀([buff:{buffId}])은 실행 중처럼 한 번 더 해석
        string once = LocalizedTextFormatter.Format(text, languageCode, args, Resolve);
        return LocalizedTextFormatter.Format(once, languageCode, null, Resolve);
    }

    // 언어 복수형 규칙에 필요한 분기가 빠졌는지 (예: ru one · few · many)
    private static IEnumerable<string> MissingPluralCategories(string text, string languageCode)
    {
        string[] required = RequiredCategories(languageCode);
        foreach (Match match in PluralBlockPattern.Matches(text))
        {
            int open = match.Index;
            int close = FindClose(text, open);
            if (close < 0)
            {
                yield return $"복수형 {match.Groups["name"].Value}의 괄호가 닫히지 않음";
                continue;
            }

            string block = text.Substring(open, close - open + 1);
            HashSet<string> selectors = new(Regex.Matches(block, @"(?<=[\s,}])(?<sel>=\d+|zero|one|two|few|many|other)\s*\{").Cast<Match>().Select(m => m.Groups["sel"].Value));
            if (!selectors.Contains(PluralRules.Other))
            {
                yield return $"복수형 {match.Groups["name"].Value}에 other 분기 없음";
            }

            foreach (string category in required)
            {
                // =1 같은 정확한 값 분기가 그 분류의 수를 모두 덮으면 통과 (en one = 1뿐)
                if (!selectors.Contains(category) && !IsCoveredByExactValues(category, selectors, languageCode))
                {
                    yield return $"복수형 {match.Groups["name"].Value}에 {category} 분기 없음";
                }
            }
        }
    }

    private static bool IsCoveredByExactValues(string category, HashSet<string> selectors, string languageCode)
    {
        for (int n = 0; n <= 200; n++)
        {
            if (PluralRules.GetCategory(languageCode, n) == category && !selectors.Contains("=" + n))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] RequiredCategories(string languageCode)
    {
        return PluralRules.GetPrimaryLanguage(languageCode) switch
        {
            "ru" or "uk" or "be" or "pl" => new[] { PluralRules.One, PluralRules.Few, PluralRules.Many },
            "cs" or "sk" => new[] { PluralRules.One, PluralRules.Few },
            "ko" or "ja" or "zh" => Array.Empty<string>(),
            _ => new[] { PluralRules.One }
        };
    }

    private static int FindClose(string text, int open)
    {
        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}' && --depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static HashSet<(string, int)> ReferenceSet(string text)
    {
        return new HashSet<(string, int)>(LocalizedTextFormatter.ExtractReferences(text).Select(reference => (reference.Kind, reference.Id)));
    }

    private static string FormatRefs(HashSet<(string kind, int id)> refs)
    {
        return "[" + string.Join(", ", refs.OrderBy(r => r.kind).ThenBy(r => r.id).Select(r => $"{r.kind}:{r.id}")) + "]";
    }

    // 표시 문구 덮어쓰기를 뺀 토큰 · 자리표시는 한글 검사에서 제외
    private static string StripTokens(string text)
    {
        return Regex.Replace(text, @"\[[a-z]+:\d+\]|\{[A-Za-z0-9_]+\}", string.Empty);
    }

    private static bool ContainsHangul(string text)
    {
        foreach (char c in text)
        {
            if ((c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㆎ'))
            {
                return true;
            }
        }

        return false;
    }
}

public enum LocalizationIssueKind
{
    Missing,
    Orphan,
    Arguments,
    References,
    Untranslated,
    Quote,
    Plural,
    Render
}

public readonly struct LocalizationIssue
{
    public string Language { get; }
    public string Key { get; }
    public LocalizationIssueKind Kind { get; }
    public string Message { get; }

    public LocalizationIssue(string language, string key, LocalizationIssueKind kind, string message)
    {
        Language = language;
        Key = key;
        Kind = kind;
        Message = message;
    }

    public override string ToString()
    {
        return $"[{Language}] {Kind} {Key}: {Message}";
    }
}
