using System.Collections.Generic;

/// <summary>
/// 몬스터 의도로 툴팁 문구를 만듭니다
/// </summary>
public static class IntentTooltipTextBuilder
{
    public static string GetIntentName(MonsterIntentIconType iconType)
    {
        return iconType switch
        {
            MonsterIntentIconType.Attack => "공격",
            MonsterIntentIconType.Protection => "방어",
            MonsterIntentIconType.BeneficialEffect => "강화",
            MonsterIntentIconType.HarmfulEffect => "약화",
            MonsterIntentIconType.Summon => "소환",
            MonsterIntentIconType.Bomb => "자폭",
            MonsterIntentIconType.DisruptCard => "방해",
            MonsterIntentIconType.Heal => "회복",
            MonsterIntentIconType.Stun => "기절",
            MonsterIntentIconType.Leave => "이탈",
            _ => string.Empty
        };
    }

    public static string Build(IReadOnlyList<MonsterIntentLine> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return string.Empty;
        }

        List<(string intentName, string sentence)> formattedLines = new(lines.Count);
        foreach (MonsterIntentLine line in lines)
        {
            formattedLines.Add((BuildIntentName(line.IconTypes), line.Sentence));
        }

        return IntentTooltipFormatter.FormatLines(formattedLines, TooltipView.Colors, BuffTooltipTextBuilder.TryResolveReference);
    }

    // 특수 패턴처럼 한 문장에 여러 의도가 섞이면 이름을 이어서 표시
    private static string BuildIntentName(IReadOnlyList<MonsterIntentIconType> iconTypes)
    {
        if (iconTypes == null || iconTypes.Count == 0)
        {
            return string.Empty;
        }

        List<string> names = new(iconTypes.Count);
        foreach (MonsterIntentIconType iconType in iconTypes)
        {
            string name = GetIntentName(iconType);
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
            {
                names.Add(name);
            }
        }

        return string.Join(" · ", names);
    }
}
