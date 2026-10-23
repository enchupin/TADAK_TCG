using System.Collections.Generic;

/// <summary>
/// 몬스터 의도로 툴팁 문구를 만듭니다
/// </summary>
public static class IntentTooltipTextBuilder
{
    // 의도 이름 (ko: 공격 · 방어 · 강화 · 약화 · 소환 · 자폭 · 방해 · 회복 · 기절 · 이탈)
    public static string GetIntentName(MonsterIntentIconType iconType)
    {
        string key = iconType switch
        {
            MonsterIntentIconType.Attack => "intent.name.attack",
            MonsterIntentIconType.Protection => "intent.name.protection",
            MonsterIntentIconType.BeneficialEffect => "intent.name.beneficialEffect",
            MonsterIntentIconType.HarmfulEffect => "intent.name.harmfulEffect",
            MonsterIntentIconType.Summon => "intent.name.summon",
            MonsterIntentIconType.Bomb => "intent.name.bomb",
            MonsterIntentIconType.DisruptCard => "intent.name.disruptCard",
            MonsterIntentIconType.Heal => "intent.name.heal",
            MonsterIntentIconType.Stun => "intent.name.stun",
            MonsterIntentIconType.Leave => "intent.name.leave",
            _ => null
        };
        return key != null ? GameText.Ui(key) : string.Empty;
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

        return IntentTooltipFormatter.FormatLines(formattedLines, TooltipView.Colors, BuffTooltipTextBuilder.TryResolveReference, BuffTooltipTextBuilder.TryResolveBuffId);
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
