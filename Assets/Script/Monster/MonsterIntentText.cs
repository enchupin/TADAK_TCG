using System.Collections.Generic;

/// <summary>
/// 몬스터 이름 · 의도 문장의 현재 언어 문구
/// </summary>
// 의도 툴팁 줄은 [buff:id] 참조를 남겨 툴팁에서 극성 색을 입히고, 의도 설명은 참조를 이름으로 풀어 씀
// 키는 Localization/<언어>/intents.json의 intent.line.* · intent.name.*
public static class MonsterIntentText
{
    // 툴팁 줄: 인자 · 복수형만 해석하고 참조 토큰은 남김
    public static string Line(string key, IReadOnlyDictionary<string, object> args = null)
    {
        return LocalizationManager.GetText(key, args);
    }

    // 의도 설명: 참조를 현재 언어 이름으로
    public static string Plain(string line)
    {
        return GameText.FormatPlain(line);
    }

    public static string MonsterName(MonsterDefinition definition)
    {
        return definition != null ? GameText.MonsterName(definition.id, definition.name) : string.Empty;
    }

    public static string PatternText(MonsterPatternDefinition pattern)
    {
        return pattern != null ? GameText.MonsterPatternText(pattern.id, pattern.text) : string.Empty;
    }
}
