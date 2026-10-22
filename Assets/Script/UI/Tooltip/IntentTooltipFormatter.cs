using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 의도 툴팁 문구 포맷 (Unity 의존 없음)
/// </summary>
// 행동 종류마다 의도 이름(올리브) 한 줄 + 문장, 행동 종류 사이는 빈 줄
// 문장 안의 숫자는 아프리콧, (부식) 같은 버프 표기는 괄호를 빼고 극성 색
public static class IntentTooltipFormatter
{
    private static readonly Regex TokenPattern = new(@"\((?<ref>[^()]+)\)|(?<num>\d+)", RegexOptions.Compiled);

    public static string FormatLines(
        IReadOnlyList<(string intentName, string sentence)> lines,
        TooltipColors colors,
        BuffTooltipFormatter.ReferenceLookup referenceLookup = null)
    {
        if (lines == null || lines.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach ((string intentName, string sentence) in lines)
        {
            string body = FormatSentence(sentence, colors, referenceLookup);
            if (string.IsNullOrWhiteSpace(intentName) && string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            if (string.IsNullOrWhiteSpace(intentName))
            {
                builder.Append(body);
                continue;
            }

            builder.Append(TooltipRichText.IntentName(intentName, colors));
            if (!string.IsNullOrWhiteSpace(body))
            {
                builder.Append('\n');
                builder.Append(body);
            }
        }

        return builder.ToString();
    }

    public static string FormatSentence(string sentence, TooltipColors colors, BuffTooltipFormatter.ReferenceLookup referenceLookup = null)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return string.Empty;
        }

        // 한 번에 치환해 색 태그 안의 16진수 값이 숫자로 다시 해석되지 않게 함
        return TokenPattern.Replace(sentence.Trim(), match =>
        {
            Group referenceGroup = match.Groups["ref"];
            if (referenceGroup.Success)
            {
                string referenceName = referenceGroup.Value.Trim();
                return referenceLookup != null && referenceLookup(referenceName, out bool harmful)
                    ? TooltipRichText.EffectName(referenceName, harmful, colors)
                    : match.Value;
            }

            return TooltipRichText.Value(match.Value, colors);
        });
    }
}
