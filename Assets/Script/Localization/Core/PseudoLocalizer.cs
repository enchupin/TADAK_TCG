using System.Text;

/// <summary>
/// 의사 언어 변환 (개발용, Unity 의존 없음)
/// </summary>
// 영어 문구의 라틴 문자를 악센트 문자로 바꾸고 길이를 약 40% 늘린 뒤 [[ ]]로 감쌈
// 화면에서 [[ ]]가 없는 문구 = 로컬라이징을 거치지 않은 하드코딩, 잘린 문구 = 칸 넘침 (독일어 · 러시아어 대비)
// {인자} · [참조] · <리치 텍스트> 안쪽은 그대로 둠
public static class PseudoLocalizer
{
    private const string Plain = "ACDEGHIKLNOSTUYZacdeghiklnostuyz";
    private const string Accented = "ÅÇÐÉĜĤÎĶĹÑÖŠŢÛÝŽåçðéĝĥîķĺñöšţûýž";
    private const double ExpansionRatio = 0.4;

    public static string Transform(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        StringBuilder builder = new(text.Length * 2 + 8);
        builder.Append("[[");
        int visible = 0;
        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            int skip = c switch
            {
                '{' => SpanLength(text, i, '{', '}'),
                '[' => SpanLength(text, i, '[', ']'),
                '<' => SpanLength(text, i, '<', '>'),
                _ => 0
            };

            if (skip > 0)
            {
                builder.Append(text, i, skip);
                i += skip;
                continue;
            }

            int index = Plain.IndexOf(c);
            builder.Append(index >= 0 ? Accented[index] : c);
            if (!char.IsWhiteSpace(c))
            {
                visible++;
            }

            i++;
        }

        int padding = (int)System.Math.Ceiling(visible * ExpansionRatio);
        if (padding > 0)
        {
            builder.Append(' ').Append('~', padding);
        }

        builder.Append("]]");
        return builder.ToString();
    }

    // 여는 기호부터 짝이 맞는 닫는 기호까지 길이 (짝이 없으면 0)
    private static int SpanLength(string text, int start, char open, char close)
    {
        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == open)
            {
                depth++;
            }
            else if (text[i] == close && --depth == 0)
            {
                return i - start + 1;
            }
        }

        return 0;
    }
}
