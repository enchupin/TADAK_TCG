using System.Text.RegularExpressions;

/// <summary>
/// 한국어 조사를 앞 단어의 받침에 맞춰 고릅니다 (Unity 의존 없음)
/// </summary>
// 한글 음절은 받침 유무, 숫자는 한국어 읽기(1 일, 2 이 …), 로마 숫자(I~X)는 몬스터 계층 표기용
// 받침을 알 수 없는 단어(영문 등)는 작성된 조사를 그대로 둠
public static class KoreanJosa
{
    private const int HangulStart = 0xAC00;
    private const int HangulEnd = 0xD7A3;
    private const int RieulFinal = 8;

    private static readonly Regex RichTextTagPattern = new(@"<[^<>]*>", RegexOptions.Compiled);

    // 긴 형태부터 비교 (을(를) → 을 순)
    private static readonly string[] JosaForms =
    {
        "을(를)", "(을)를", "를(을)", "이(가)", "(이)가", "가(이)", "은(는)", "(은)는", "는(은)",
        "과(와)", "와(과)", "(과)와", "(으)로", "으로",
        "을", "를", "이", "가", "은", "는", "과", "와", "로"
    };

    // 숫자 끝자리 읽기: 0 영, 1 일, 2 이, 3 삼, 4 사, 5 오, 6 육, 7 칠, 8 팔, 9 구
    private static readonly bool[] DigitHasFinal = { true, true, false, true, false, false, true, true, true, false };
    private static readonly bool[] DigitIsRieul = { false, true, false, false, false, false, false, true, true, false };

    public static bool TryGetFinalConsonant(string word, out bool hasFinal, out bool isRieul)
    {
        hasFinal = false;
        isRieul = false;
        string plain = StripForJudgement(word);
        if (plain.Length == 0)
        {
            return false;
        }

        char last = plain[plain.Length - 1];
        if (last >= HangulStart && last <= HangulEnd)
        {
            int finalConsonant = (last - HangulStart) % 28;
            hasFinal = finalConsonant != 0;
            isRieul = finalConsonant == RieulFinal;
            return true;
        }

        if (last >= '0' && last <= '9')
        {
            int digit = last - '0';
            // 10 십, 100 백, 1000 천, 10000 만은 모두 받침 있음 (ㄹ 아님)
            if (digit == 0 && plain.Length >= 2 && char.IsDigit(plain[plain.Length - 2]))
            {
                hasFinal = true;
                return true;
            }

            hasFinal = DigitHasFinal[digit];
            isRieul = DigitIsRieul[digit];
            return true;
        }

        return TryGetRomanNumeralFinal(plain, out hasFinal, out isRieul);
    }

    // 받침에 맞는 조사 형태 (을/를 계열 어느 쪽을 넘겨도 됨)
    public static string Select(string word, string josa)
    {
        if (string.IsNullOrEmpty(josa))
        {
            return josa ?? string.Empty;
        }

        if (!TryGetFinalConsonant(word, out bool hasFinal, out bool isRieul))
        {
            return josa;
        }

        switch (josa)
        {
            case "을":
            case "를":
            case "을(를)":
            case "(을)를":
            case "를(을)":
                return hasFinal ? "을" : "를";
            case "이":
            case "가":
            case "이(가)":
            case "(이)가":
            case "가(이)":
                return hasFinal ? "이" : "가";
            case "은":
            case "는":
            case "은(는)":
            case "(은)는":
            case "는(은)":
                return hasFinal ? "은" : "는";
            case "과":
            case "와":
            case "과(와)":
            case "와(과)":
            case "(과)와":
                return hasFinal ? "과" : "와";
            case "으로":
            case "로":
            case "(으)로":
                // ㄹ 받침은 으로가 아니라 로
                return hasFinal && !isRieul ? "으로" : "로";
            default:
                return josa;
        }
    }

    // 단어 + 조사
    public static string Attach(string word, string josa)
    {
        return (word ?? string.Empty) + Select(word, josa);
    }

    // text의 index 위치에서 시작하는 조사 형태 길이 (없으면 0)
    public static int MatchJosaAt(string text, int index, out string josa)
    {
        josa = string.Empty;
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
        {
            return 0;
        }

        foreach (string form in JosaForms)
        {
            if (string.CompareOrdinal(text, index, form, 0, form.Length) == 0)
            {
                josa = form;
                return form.Length;
            }
        }

        return 0;
    }

    // 리치 텍스트 태그와 끝의 닫는 괄호·따옴표·공백은 받침 판정에서 제외
    private static string StripForJudgement(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return string.Empty;
        }

        string plain = word.IndexOf('<') >= 0 ? RichTextTagPattern.Replace(word, string.Empty) : word;
        int end = plain.Length;
        while (end > 0)
        {
            char c = plain[end - 1];
            if (char.IsWhiteSpace(c) || c == ')' || c == ']' || c == '"' || c == '\'' || c == '」' || c == '』')
            {
                end--;
                continue;
            }

            break;
        }

        return plain.Substring(0, end);
    }

    // 끝 단어가 로마 숫자(I~X)이면 한국어 읽기로 판정 (변이 꽃 II → 이)
    private static bool TryGetRomanNumeralFinal(string plain, out bool hasFinal, out bool isRieul)
    {
        hasFinal = false;
        isRieul = false;
        int start = plain.Length;
        while (start > 0 && "IVX".IndexOf(plain[start - 1]) >= 0)
        {
            start--;
        }

        if (start == plain.Length || (start > 0 && !char.IsWhiteSpace(plain[start - 1])))
        {
            return false;
        }

        switch (plain.Substring(start))
        {
            case "I":
            case "VII":
            case "VIII":
                // 일 · 칠 · 팔
                hasFinal = true;
                isRieul = true;
                return true;
            case "III":
            case "VI":
            case "X":
                // 삼 · 육 · 십
                hasFinal = true;
                return true;
            case "II":
            case "IV":
            case "V":
            case "IX":
                // 이 · 사 · 오 · 구
                return true;
            default:
                return false;
        }
    }
}
