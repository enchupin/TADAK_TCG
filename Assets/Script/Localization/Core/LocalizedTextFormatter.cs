using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 로컬라이징 문구의 인자 · 복수형 · 참조 토큰을 해석합니다 (Unity 의존 없음)
/// </summary>
// {name}: 인자 값, 인자가 없으면 원문 그대로 둠 (카드의 {attack} 등은 이후 단계에서 처리)
// {n, plural, =1 {…} one {…} few {…} many {…} other {…}}: ICU 복수형, # = 수
// {key, select, a {…} other {…}}: ICU 선택
// [buff:3012] · [buff:3012|표시 문구] · [keyword:1] · [card:101010]: 참조 토큰, 표시 문구는 격변화 등 덮어쓰기용
// 한국어는 인자 · 참조 바로 뒤의 조사(을/를 등)를 치환 결과의 받침에 맞춤
// ICU 작은따옴표 이스케이프는 지원하지 않음 (프랑스어 d'Armure 등을 그대로 쓰기 위함)
// substituteSimpleArguments가 false면 복수형 · 선택만 해석하고 {name}은 남김 (툴팁의 {stack} 색 처리용)
public static class LocalizedTextFormatter
{
    public delegate bool ReferenceResolver(LocalizedReference reference, out string text);

    private const string KoreanLanguage = "ko";

    public static string Format(
        string template,
        string languageCode,
        IReadOnlyDictionary<string, object> args = null,
        ReferenceResolver referenceResolver = null,
        bool substituteSimpleArguments = true)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template ?? string.Empty;
        }

        if (template.IndexOf('{') < 0 && template.IndexOf('[') < 0)
        {
            return template;
        }

        bool adjustJosa = PluralRules.GetPrimaryLanguage(languageCode) == KoreanLanguage;
        return FormatInternal(template, new FormatContext(languageCode, args, referenceResolver, adjustJosa, substituteSimpleArguments));
    }

    // 문구에 들어 있는 참조 토큰 목록 (카드 툴팁의 참조 버프 추출 · 검증용)
    public static List<LocalizedReference> ExtractReferences(string template)
    {
        List<LocalizedReference> references = new();
        if (string.IsNullOrEmpty(template))
        {
            return references;
        }

        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '[' && TryParseReference(template, i, out LocalizedReference reference, out int length))
            {
                references.Add(reference);
                i += length - 1;
            }
        }

        return references;
    }

    // 문구에 쓰인 인자 이름 목록 (복수형 · 선택 안의 인자 포함, 검증용)
    public static HashSet<string> ExtractArgumentNames(string template)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        CollectArgumentNames(template, names);
        return names;
    }

    private readonly struct FormatContext
    {
        public readonly string LanguageCode;
        public readonly IReadOnlyDictionary<string, object> Args;
        public readonly ReferenceResolver ReferenceResolver;
        public readonly bool AdjustJosa;
        public readonly bool SubstituteSimpleArguments;

        public FormatContext(string languageCode, IReadOnlyDictionary<string, object> args, ReferenceResolver referenceResolver, bool adjustJosa, bool substituteSimpleArguments)
        {
            LanguageCode = languageCode;
            Args = args;
            ReferenceResolver = referenceResolver;
            AdjustJosa = adjustJosa;
            SubstituteSimpleArguments = substituteSimpleArguments;
        }
    }

    private static string FormatInternal(string template, FormatContext context)
    {
        StringBuilder builder = new(template.Length + 16);
        int i = 0;
        while (i < template.Length)
        {
            char c = template[i];
            string replaced = null;
            int consumed = 0;

            if (c == '{')
            {
                int close = FindClosingBrace(template, i);
                if (close > i)
                {
                    consumed = close - i + 1;
                    replaced = FormatArgument(template.Substring(i + 1, close - i - 1), context);
                }
            }
            else if (c == '[' && TryParseReference(template, i, out LocalizedReference reference, out int length))
            {
                consumed = length;
                if (context.ReferenceResolver != null && context.ReferenceResolver(reference, out string resolved))
                {
                    replaced = resolved ?? string.Empty;
                }
                else if (reference.HasDisplayOverride)
                {
                    replaced = reference.DisplayOverride;
                }
            }

            if (consumed == 0)
            {
                builder.Append(c);
                i++;
                continue;
            }

            if (replaced == null)
            {
                // 해석하지 못한 자리표시는 원문 그대로
                builder.Append(template, i, consumed);
                i += consumed;
                continue;
            }

            builder.Append(replaced);
            i += consumed;
            if (context.AdjustJosa)
            {
                int josaLength = KoreanJosa.MatchJosaAt(template, i, out string josa);
                if (josaLength > 0)
                {
                    builder.Append(KoreanJosa.Select(replaced, josa));
                    i += josaLength;
                }
            }
        }

        return builder.ToString();
    }

    // { } 안쪽: 이름 또는 "이름, plural|select, 분기들"
    private static string FormatArgument(string content, FormatContext context)
    {
        IReadOnlyDictionary<string, object> args = context.Args;
        int firstComma = IndexOfTopLevel(content, ',', 0);
        if (firstComma < 0)
        {
            string name = content.Trim();
            return context.SubstituteSimpleArguments && args != null && args.TryGetValue(name, out object value) ? ToDisplayString(value) : null;
        }

        string argumentName = content.Substring(0, firstComma).Trim();
        int secondComma = IndexOfTopLevel(content, ',', firstComma + 1);
        if (secondComma < 0 || args == null || !args.TryGetValue(argumentName, out object argumentValue))
        {
            return null;
        }

        string formatType = content.Substring(firstComma + 1, secondComma - firstComma - 1).Trim();
        Dictionary<string, string> branches = ParseBranches(content.Substring(secondComma + 1));
        if (branches == null)
        {
            return null;
        }

        string chosen;
        switch (formatType)
        {
            case "plural":
                if (!TryToNumber(argumentValue, out long number))
                {
                    return null;
                }

                chosen = ChoosePluralBranch(branches, context.LanguageCode, number);
                if (chosen == null)
                {
                    return null;
                }

                chosen = ReplaceTopLevelHash(chosen, ToDisplayString(argumentValue));
                break;

            case "select":
                string key = ToDisplayString(argumentValue);
                if (!branches.TryGetValue(key, out chosen) && !branches.TryGetValue(PluralRules.Other, out chosen))
                {
                    return null;
                }

                break;

            default:
                return null;
        }

        return FormatInternal(chosen, context);
    }

    private static string ChoosePluralBranch(Dictionary<string, string> branches, string languageCode, long number)
    {
        if (branches.TryGetValue("=" + number.ToString(CultureInfo.InvariantCulture), out string exact))
        {
            return exact;
        }

        string category = PluralRules.GetCategory(languageCode, number);
        if (branches.TryGetValue(category, out string categorized))
        {
            return categorized;
        }

        return branches.TryGetValue(PluralRules.Other, out string other) ? other : null;
    }

    // "one {카드} other {카드들}" → 선택자별 문구
    private static Dictionary<string, string> ParseBranches(string text)
    {
        Dictionary<string, string> branches = new(StringComparer.Ordinal);
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (i >= text.Length)
            {
                break;
            }

            int selectorStart = i;
            while (i < text.Length && text[i] != '{' && !char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            string selector = text.Substring(selectorStart, i - selectorStart);
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            if (selector.Length == 0 || i >= text.Length || text[i] != '{')
            {
                return null;
            }

            int close = FindClosingBrace(text, i);
            if (close < 0)
            {
                return null;
            }

            branches[selector] = text.Substring(i + 1, close - i - 1);
            i = close + 1;
        }

        return branches.Count > 0 ? branches : null;
    }

    // 중첩된 { } 안의 #은 안쪽 복수형 몫이므로 바꾸지 않음
    private static string ReplaceTopLevelHash(string text, string value)
    {
        if (text.IndexOf('#') < 0)
        {
            return text;
        }

        StringBuilder builder = new(text.Length + 8);
        int depth = 0;
        foreach (char c in text)
        {
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }

            if (c == '#' && depth == 0)
            {
                builder.Append(value);
                continue;
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static int FindClosingBrace(string text, int openIndex)
    {
        int depth = 0;
        for (int i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static int IndexOfTopLevel(string text, char target, int start)
    {
        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }
            else if (c == target && depth == 0)
            {
                return i;
            }
        }

        return -1;
    }

    // [kind:id] 또는 [kind:id|표시 문구], kind는 영문 소문자
    private static bool TryParseReference(string text, int start, out LocalizedReference reference, out int length)
    {
        reference = default;
        length = 0;
        int i = start + 1;
        int kindStart = i;
        while (i < text.Length && text[i] >= 'a' && text[i] <= 'z')
        {
            i++;
        }

        if (i == kindStart || i >= text.Length || text[i] != ':')
        {
            return false;
        }

        string kind = text.Substring(kindStart, i - kindStart);
        i++;
        int idStart = i;
        while (i < text.Length && char.IsDigit(text[i]))
        {
            i++;
        }

        if (i == idStart || i >= text.Length || !int.TryParse(text.Substring(idStart, i - idStart), NumberStyles.None, CultureInfo.InvariantCulture, out int id))
        {
            return false;
        }

        string displayOverride = null;
        if (text[i] == '|')
        {
            int close = text.IndexOf(']', i + 1);
            if (close < 0)
            {
                return false;
            }

            displayOverride = text.Substring(i + 1, close - i - 1);
            i = close;
        }

        if (text[i] != ']')
        {
            return false;
        }

        reference = new LocalizedReference(kind, id, displayOverride);
        length = i - start + 1;
        return true;
    }

    private static void CollectArgumentNames(string template, HashSet<string> names)
    {
        if (string.IsNullOrEmpty(template))
        {
            return;
        }

        int i = 0;
        while (i < template.Length)
        {
            if (template[i] != '{')
            {
                i++;
                continue;
            }

            int close = FindClosingBrace(template, i);
            if (close < 0)
            {
                return;
            }

            string content = template.Substring(i + 1, close - i - 1);
            int firstComma = IndexOfTopLevel(content, ',', 0);
            if (firstComma < 0)
            {
                names.Add(content.Trim());
            }
            else
            {
                names.Add(content.Substring(0, firstComma).Trim());
                int secondComma = IndexOfTopLevel(content, ',', firstComma + 1);
                Dictionary<string, string> branches = secondComma >= 0 ? ParseBranches(content.Substring(secondComma + 1)) : null;
                if (branches != null)
                {
                    foreach (string branch in branches.Values)
                    {
                        CollectArgumentNames(branch, names);
                    }
                }
            }

            i = close + 1;
        }
    }

    private static bool TryToNumber(object value, out long number)
    {
        switch (value)
        {
            case int intValue:
                number = intValue;
                return true;
            case long longValue:
                number = longValue;
                return true;
            case short shortValue:
                number = shortValue;
                return true;
            case float floatValue:
                number = (long)Math.Floor(floatValue);
                return true;
            case double doubleValue:
                number = (long)Math.Floor(doubleValue);
                return true;
            case string text:
                return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
            default:
                number = 0;
                return false;
        }
    }

    private static string ToDisplayString(object value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}

/// <summary>
/// 문구 안의 참조 토큰 ([buff:3012|표시 문구])
/// </summary>
public readonly struct LocalizedReference
{
    public const string Buff = "buff";
    public const string Keyword = "keyword";
    public const string Card = "card";
    public const string Character = "character";

    public string Kind { get; }
    public int Id { get; }
    public string DisplayOverride { get; }
    public bool HasDisplayOverride => !string.IsNullOrEmpty(DisplayOverride);

    public LocalizedReference(string kind, int id, string displayOverride)
    {
        Kind = kind ?? string.Empty;
        Id = id;
        DisplayOverride = displayOverride;
    }
}
