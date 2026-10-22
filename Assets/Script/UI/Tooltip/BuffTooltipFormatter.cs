using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 버프 툴팁 문구 포맷 (Unity 의존 없음)
/// </summary>
// 이름 한 줄 + 설명 형식, 이름은 극성 색 (패시브는 이로운 효과와 같은 색)
// {stack} · {stack장} · {stack회}: 현재 중첩 수치(아프리콧) + 단위
// {동력} 같은 동적 이름: 조회 결과 이름으로 바꾸고 뒤따르는 조사를 받침에 맞춤
// (부식) 같은 참조 버프: 괄호를 빼고 참조 버프의 극성 색, 버프가 아닌 괄호 문구는 그대로 둠
// [buff:4001] · [buff:4001|표시 문구]: 로컬라이징 표의 ID 참조, 현재 언어 이름(또는 표시 문구) + 극성 색, 뒤따르는 조사 보정
public static class BuffTooltipFormatter
{
    public delegate bool ReferenceLookup(string buffName, out bool harmful);
    public delegate bool DynamicNameLookup(string key, out string buffName, out bool harmful);
    public delegate bool BuffIdLookup(int buffId, out string buffName, out bool harmful);

    private const string StackKey = "stack";

    private static readonly Regex TokenPattern = new(
        @"\{(?<key>[^{}]+)\}(?<josa>으로|로|을|를|이|가|은|는|과|와)?|\[buff:(?<id>\d+)(?:\|(?<display>[^\]]*))?\](?<josa>으로|로|을|를|이|가|은|는|과|와)?|\((?<ref>[^()]+)\)",
        RegexOptions.Compiled);

    public static string FormatEntry(
        string name,
        string description,
        int stack,
        bool harmful,
        TooltipColors colors,
        ReferenceLookup referenceLookup = null,
        DynamicNameLookup dynamicLookup = null,
        BuffIdLookup idLookup = null)
    {
        string coloredName = TooltipRichText.EffectName(name, harmful, colors);
        string body = FormatDescription(description, stack, colors, referenceLookup, dynamicLookup, idLookup);
        if (string.IsNullOrWhiteSpace(name))
        {
            return body;
        }

        // 이름이 항상 한 줄을 차지하도록 설명은 다음 줄부터
        return string.IsNullOrWhiteSpace(body) ? coloredName : $"{coloredName}\n{body}";
    }

    public static string FormatDescription(
        string description,
        int stack,
        TooltipColors colors,
        ReferenceLookup referenceLookup = null,
        DynamicNameLookup dynamicLookup = null,
        BuffIdLookup idLookup = null)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // 한 번에 치환해 새로 넣은 색 태그가 다시 해석되지 않게 함
        return TokenPattern.Replace(description.Trim(), match =>
        {
            Group idGroup = match.Groups["id"];
            if (idGroup.Success)
            {
                string idJosa = match.Groups["josa"].Value;
                Group displayGroup = match.Groups["display"];
                string display = displayGroup.Success && displayGroup.Value.Length > 0 ? displayGroup.Value : null;
                if (idLookup != null && int.TryParse(idGroup.Value, out int buffId)
                    && idLookup(buffId, out string buffName, out bool idHarmful) && !string.IsNullOrEmpty(display ?? buffName))
                {
                    string shown = display ?? buffName;
                    return TooltipRichText.EffectName(shown, idHarmful, colors) + AdjustJosa(shown, idJosa);
                }

                // 조회할 수 없으면 표시 문구만, 그것도 없으면 원문
                return display != null ? display + AdjustJosa(display, idJosa) : match.Value;
            }

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

    // 족보 한 줄: 이름: 조건 · 조건 → 보상 · 보상
    // 달성 조건은 기본 색, 미달성 조건은 회색, 보상을 더 받을 수 없으면 조건과 보상 모두 회색
    // 구분자( · , → )는 항상 기본 색
    public static string FormatProgressEntry(
        string name,
        IReadOnlyList<(string text, bool achieved)> conditions,
        IReadOnlyList<string> rewards,
        bool rewardClaimed,
        TooltipColors colors)
    {
        StringBuilder builder = new();
        builder.Append(TooltipRichText.EffectName(name, false, colors));
        builder.Append(": ");

        if (conditions != null)
        {
            for (int i = 0; i < conditions.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" · ");
                }

                (string text, bool achieved) = conditions[i];
                builder.Append(achieved && !rewardClaimed ? text : TooltipRichText.Inactive(text, colors));
            }
        }

        builder.Append(" → ");
        if (rewards != null)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" · ");
                }

                builder.Append(rewardClaimed ? TooltipRichText.Inactive(rewards[i], colors) : rewards[i]);
            }
        }

        return builder.ToString();
    }

    // 이름의 마지막 글자 받침에 맞춰 조사 선택
    public static string AdjustJosa(string word, string josa)
    {
        // 받침 판정 · ㄹ 받침 으로/로 처리는 KoreanJosa로 통합
        return KoreanJosa.Select(word, josa);
    }
}
