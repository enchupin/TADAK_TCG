using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 의도 툴팁 문구 포맷 (Unity 의존 없음)
/// </summary>
// 행동 종류마다 의도 이름(올리브) 한 줄 + 문장, 행동 종류 사이는 빈 줄
// 문장 안의 숫자는 아프리콧, (부식) 같은 버프 표기는 괄호를 빼고 극성 색
// [buff:4001] · [buff:4001|표시 문구]: 현재 언어 이름(또는 표시 문구) + 극성 색, 뒤따르는 조사 보정 (ID 숫자는 수치로 보지 않음)
public static class IntentTooltipFormatter
{
    private static readonly Regex TokenPattern = new(
        @"\[buff:(?<id>\d+)(?:\|(?<display>[^\]]*))?\](?<josa>으로|로|을|를|이|가|은|는|과|와)?|\((?<ref>[^()]+)\)|(?<num>\d+)",
        RegexOptions.Compiled);

    public static string FormatLines(
        IReadOnlyList<(string intentName, string sentence)> lines,
        TooltipColors colors,
        BuffTooltipFormatter.ReferenceLookup referenceLookup = null,
        BuffTooltipFormatter.BuffIdLookup idLookup = null)
    {
        if (lines == null || lines.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        foreach ((string intentName, string sentence) in lines)
        {
            string body = FormatSentence(sentence, colors, referenceLookup, idLookup);
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

    public static string FormatSentence(
        string sentence,
        TooltipColors colors,
        BuffTooltipFormatter.ReferenceLookup referenceLookup = null,
        BuffTooltipFormatter.BuffIdLookup idLookup = null)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return string.Empty;
        }

        // 한 번에 치환해 색 태그 안의 16진수 값이 숫자로 다시 해석되지 않게 함
        return TokenPattern.Replace(sentence.Trim(), match =>
        {
            Group idGroup = match.Groups["id"];
            if (idGroup.Success)
            {
                string josa = match.Groups["josa"].Value;
                Group displayGroup = match.Groups["display"];
                string display = displayGroup.Success && displayGroup.Value.Length > 0 ? displayGroup.Value : null;
                if (idLookup != null && int.TryParse(idGroup.Value, out int buffId)
                    && idLookup(buffId, out string buffName, out bool idHarmful) && !string.IsNullOrEmpty(display ?? buffName))
                {
                    string shown = display ?? buffName;
                    return TooltipRichText.EffectName(shown, idHarmful, colors) + BuffTooltipFormatter.AdjustJosa(shown, josa);
                }

                return display != null ? display + BuffTooltipFormatter.AdjustJosa(display, josa) : match.Value;
            }

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
