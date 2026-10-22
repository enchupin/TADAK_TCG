/// <summary>
/// 언어별 복수형 분류 (CLDR 정수 규칙, Unity 의존 없음)
/// </summary>
// 분류 이름은 ICU 복수형과 같음: zero · one · two · few · many · other
// 목록에 없는 언어는 영어 규칙(1 = one, 나머지 other)을 사용
public static class PluralRules
{
    public const string Zero = "zero";
    public const string One = "one";
    public const string Two = "two";
    public const string Few = "few";
    public const string Many = "many";
    public const string Other = "other";

    public static string GetCategory(string languageCode, long number)
    {
        long n = number < 0 ? -number : number;
        switch (GetPrimaryLanguage(languageCode))
        {
            // 단·복수 구분 없음
            case "ko":
            case "ja":
            case "zh":
            case "th":
            case "vi":
            case "id":
                return Other;

            // 0과 1이 단수
            case "fr":
            case "pt":
                return n <= 1 ? One : Other;

            // 1 · 2~4 · 5 이상 (11~14는 many)
            case "ru":
            case "uk":
            case "be":
                return GetEastSlavicCategory(n);

            case "pl":
                if (n == 1)
                {
                    return One;
                }

                return IsFewSlavic(n) ? Few : Many;

            case "cs":
            case "sk":
                if (n == 1)
                {
                    return One;
                }

                return n >= 2 && n <= 4 ? Few : Other;

            default:
                return n == 1 ? One : Other;
        }
    }

    // zh-Hans → zh, pt-BR → pt
    public static string GetPrimaryLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return string.Empty;
        }

        int separator = languageCode.IndexOfAny(new[] { '-', '_' });
        string primary = separator > 0 ? languageCode.Substring(0, separator) : languageCode;
        return primary.Trim().ToLowerInvariant();
    }

    private static string GetEastSlavicCategory(long n)
    {
        long mod10 = n % 10;
        long mod100 = n % 100;
        if (mod10 == 1 && mod100 != 11)
        {
            return One;
        }

        return IsFewSlavic(n) ? Few : Many;
    }

    private static bool IsFewSlavic(long n)
    {
        long mod10 = n % 10;
        long mod100 = n % 100;
        return mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14);
    }
}
