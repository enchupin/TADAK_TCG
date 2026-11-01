using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 카드 선택 결과 (선택 안내 위에 "선택된 카드는 ~" 문구로 표시)
/// </summary>
public enum CardSelectionOutcome
{
    Exhaust,
    Retain,
    Upgrade,
    ChooseUpgrade,
    MoveToDrawTop,
    Discard,
    MoveToHand,
    CopyToHand,
    AddToHand,
    ReduceCost,
    Transform,
    Fragment,
    AddToDeck,
    RemoveFromDeck,
    DuplicateInDeck
}

/// <summary>
/// 선택 결과 문구 조합 (Unity 의존 없음): 결과마다 한 문장, 여러 개면 순서대로 이어 붙임
/// </summary>
public static class CardSelectionOutcomeText
{
    public static string Key(CardSelectionOutcome outcome)
    {
        string name = outcome.ToString();
        return "ui.selection." + char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    // lookup: 키 → 현재 언어 문장 (없으면 null · 빈 문자열), 결과가 없으면 빈 문자열 (문구 숨김)
    public static string Build(IReadOnlyList<CardSelectionOutcome> outcomes, Func<string, string> lookup)
    {
        if (outcomes == null || outcomes.Count == 0 || lookup == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        HashSet<CardSelectionOutcome> added = new HashSet<CardSelectionOutcome>();
        foreach (CardSelectionOutcome outcome in outcomes)
        {
            if (!added.Add(outcome))
            {
                continue;
            }

            string sentence = lookup(Key(outcome));
            if (string.IsNullOrWhiteSpace(sentence))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }
            builder.Append(sentence.Trim());
        }

        return builder.ToString();
    }
}
