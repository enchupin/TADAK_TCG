using System.Text.RegularExpressions;

/// <summary>
/// 버프 툴팁 문구 포맷 (Unity 의존 없음)
/// </summary>
// 이름: 설명 형식, 이름은 극성 색 (패시브는 이로운 효과와 같은 색)
// {stack} · {stack장} · {stack회}: 현재 중첩 수치(아프리콧) + 단위
// {동력} 같은 동적 이름: 조회 결과 이름으로 바꾸고 뒤따르는 조사를 받침에 맞춤
// (부식) 같은 참조 버프: 괄호를 빼고 참조 버프의 극성 색, 버프가 아닌 괄호 문구는 그대로 둠
public static class BuffTooltipFormatter
{
    public delegate bool ReferenceLookup(string buffName, out bool harmful);
    public delegate bool DynamicNameLookup(string key, out string buffName, out bool harmful);

    private const string StackKey = "stack";

    private static readonly Regex TokenPattern = new(
        @"\{(?<key>[^{}]+)\}(?<josa>으로|로|을|를|이|가|은|는|과|와)?|\((?<ref>[^()]+)\)",
        RegexOptions.Compiled);

    public static string FormatEntry(
        string name,
        string description,
        int stack,
        bool harmful,
        TooltipColors colors,
        ReferenceLookup referenceLookup = null,
        DynamicNameLookup dynamicLookup = null)
    {
        string coloredName = TooltipRichText.EffectName(name, harmful, colors);
        string body = FormatDescription(description, stack, colors, referenceLookup, dynamicLookup);
        if (string.IsNullOrWhiteSpace(name))
        {
            return body;
        }

        return string.IsNullOrWhiteSpace(body) ? coloredName : $"{coloredName}: {body}";
    }

    public static string FormatDescription(
        string description,
        int stack,
        TooltipColors colors,
        ReferenceLookup referenceLookup = null,
        DynamicNameLookup dynamicLookup = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // 한 번에 치환해 새로 넣은 색 태그가 다시 해석되지 않게 함
        return TokenPattern.Replace(description.Trim(), match =>
        {
            Group referenceGroup = match.Groups["ref"];
            if (referenceGroup.Success)
            {
                string referenceName = referenceGroup.Value.Trim();
                return referenceLookup != null && referenceLookup(referenceName, out bool referenceHarmful)
                    ? TooltipRichText.EffectName(referenceName, referenceHarmful, colors)
                    : match.Value;
            }

            string key = match.Groups["key"].Value.Trim();
            string josa = match.Groups["josa"].Value;
            if (key.StartsWith(StackKey))
            {
                // {stack장}처럼 붙은 단위는 색 없이 그대로 둠
                return TooltipRichText.Value(stack, colors) + key.Substring(StackKey.Length) + josa;
            }

            if (dynamicLookup != null && dynamicLookup(key, out string dynamicName, out bool dynamicHarmful)
                && !string.IsNullOrEmpty(dynamicName))
            {
                return TooltipRichText.EffectName(dynamicName, dynamicHarmful, colors) + AdjustJosa(dynamicName, josa);
            }

            // 알 수 없는 자리표시는 중괄호만 뺌
            return key + josa;
        });
    }

    // 이름의 마지막 글자 받침에 맞춰 조사 선택
    public static string AdjustJosa(string word, string josa)
    {
        if (string.IsNullOrEmpty(josa) || string.IsNullOrEmpty(word))
        {
            return josa ?? string.Empty;
        }

        char last = word[word.Length - 1];
        if (last < '가' || last > '힣')
        {
            return josa;
        }

        int finalConsonant = (last - '가') % 28;
        bool hasFinal = finalConsonant != 0;
        // ㄹ 받침은 으로가 아니라 로
        bool isRieul = finalConsonant == 8;
        switch (josa)
        {
            case "을":
            case "를":
                return hasFinal ? "을" : "를";
            case "이":
            case "가":
                return hasFinal ? "이" : "가";
            case "은":
            case "는":
                return hasFinal ? "은" : "는";
            case "과":
            case "와":
                return hasFinal ? "과" : "와";
            case "으로":
            case "로":
                return hasFinal && !isRieul ? "으로" : "로";
            default:
                return josa;
        }
    }
}
