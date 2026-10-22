using System.Collections.Generic;

// 의도 툴팁 한 줄: 행동 종류와 문장
// 문장 안의 (버프명) 표기는 툴팁에서 괄호를 빼고 극성 색으로 표시
public sealed class MonsterIntentLine
{
    private readonly List<MonsterIntentIconType> iconTypes = new();

    public MonsterIntentLine(string sentence, params MonsterIntentIconType[] types)
    {
        Sentence = sentence ?? string.Empty;
        if (types == null)
        {
            return;
        }

        foreach (MonsterIntentIconType type in types)
        {
            if (type != MonsterIntentIconType.None && !iconTypes.Contains(type))
            {
                iconTypes.Add(type);
            }
        }
    }

    public IReadOnlyList<MonsterIntentIconType> IconTypes => iconTypes;
    public string Sentence { get; private set; }

    public bool Contains(MonsterIntentIconType type)
    {
        return iconTypes.Contains(type);
    }

    public bool IsSingle(MonsterIntentIconType type)
    {
        return iconTypes.Count == 1 && iconTypes[0] == type;
    }

    public void AppendSentence(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return;
        }

        Sentence = string.IsNullOrEmpty(Sentence) ? sentence : $"{Sentence} {sentence}";
    }

    public void ReplaceSentence(string sentence)
    {
        Sentence = sentence ?? string.Empty;
    }

    public void ReplaceIconType(MonsterIntentIconType previousType, MonsterIntentIconType nextType)
    {
        int index = iconTypes.IndexOf(previousType);
        if (index >= 0)
        {
            iconTypes[index] = nextType;
        }
    }
}
