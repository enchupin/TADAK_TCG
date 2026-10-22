// 툴팁 강조 색상 (RRGGBB 또는 RRGGBBAA, # 제외)
// Unity 의존 없이 포맷터와 테스트에서 함께 사용
public readonly struct TooltipColors
{
    public readonly string BeneficialHex;
    public readonly string HarmfulHex;
    public readonly string IntentHex;
    public readonly string ValueHex;
    // 진행도의 미달성 조건 · 이미 받은 보상
    public readonly string InactiveHex;

    public TooltipColors(string beneficialHex, string harmfulHex, string intentHex, string valueHex, string inactiveHex)
    {
        BeneficialHex = beneficialHex;
        HarmfulHex = harmfulHex;
        IntentHex = intentHex;
        ValueHex = valueHex;
        InactiveHex = inactiveHex;
    }

    // 라피스 · 스칼렛 · 올리브 · 아프리콧 · 회색
    public static TooltipColors Default => new("26619C", "FF2400", "808000", "FBCEB1", "808080");
}

/// <summary>
/// TextMeshPro 리치 텍스트 강조 헬퍼
/// </summary>
public static class TooltipRichText
{
    public static string Colorize(string text, string hex)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(hex))
        {
            return text ?? string.Empty;
        }

        return $"<color=#{hex}>{text}</color>";
    }

    // 이로운 효과와 패시브는 라피스, 해로운 효과는 스칼렛
    public static string EffectName(string name, bool harmful, TooltipColors colors)
    {
        return Colorize(name, harmful ? colors.HarmfulHex : colors.BeneficialHex);
    }

    public static string IntentName(string name, TooltipColors colors)
    {
        return Colorize(name, colors.IntentHex);
    }

    public static string Value(string value, TooltipColors colors)
    {
        return Colorize(value, colors.ValueHex);
    }

    public static string Value(int value, TooltipColors colors)
    {
        return Value(value.ToString(), colors);
    }

    public static string Inactive(string text, TooltipColors colors)
    {
        return Colorize(text, colors.InactiveHex);
    }
}
