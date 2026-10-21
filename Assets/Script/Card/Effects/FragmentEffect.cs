using System.Collections.Generic;
using UnityEngine;

// 조각: 손의 다른 카드 1장(동료 카드 포함)에 효과를 붙임
// 붙은 카드를 쓸 때 본래 효과 다음에 페이로드 카드의 효과가 발동하며 이번 전투 동안 유지 (R-73)
// 한 카드에는 조각을 1개만 새길 수 있음
public class FragmentEffect : ICardEffect
{
    // 새길 효과 (JSON의 effects)
    public List<ICardEffect> payloadEffects;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager?.handManager == null || payloadEffects == null || payloadEffects.Count == 0)
        {
            return;
        }

        Card hostCard = battleManager.battleContext?.GetContextCard("ThisCard")
            ?? battleManager.battleContext?.GetLastPlayedCard();
        List<Card> candidates = ResolveCandidates(battleManager, hostCard);
        if (candidates.Count == 0)
        {
            Debug.Log("[FragmentEffect] 조각을 새길 수 있는 카드가 없습니다");
            return;
        }

        string description = ExtractQuotedText(hostCard?.description);
        if (battleManager.OpenSelectCardPanel(candidates, 1, selectedCards => ApplyFragment(battleManager, selectedCards, description)))
        {
            return;
        }

        ApplyFragment(battleManager, new List<Card> { candidates[0] }, description);
    }

    // 조각 카드 설명의 따옴표 안 문구를 새긴 효과의 표시 문구로 사용 (현지화된 설명 기준)
    public static string ExtractQuotedText(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        foreach ((char open, char close) in new[] { ('"', '"'), ('\u201C', '\u201D'), ('\u300C', '\u300D') })
        {
            int start = description.IndexOf(open);
            int end = start >= 0 ? description.IndexOf(close, start + 1) : -1;
            if (start >= 0 && end > start)
            {
                return description.Substring(start + 1, end - start - 1).Trim();
            }
        }

        return string.Empty;
    }

    public static List<Card> ResolveCandidates(TrainingBattleManager battleManager, Card hostCard)
    {
        List<Card> candidates = new List<Card>();
        List<Card> handCards = battleManager?.handManager?.GetHandCards();
        if (handCards == null)
        {
            return candidates;
        }

        foreach (Card handCard in handCards)
        {
            if (handCard != null && handCard != hostCard && !handCard.HasFragment)
            {
                candidates.Add(handCard);
            }
        }

        return candidates;
    }

    private void ApplyFragment(TrainingBattleManager battleManager, List<Card> selectedCards, string description)
    {
        if (selectedCards == null || selectedCards.Count == 0 || selectedCards[0] == null || selectedCards[0].HasFragment)
        {
            return;
        }

        Card targetCard = selectedCards[0];
        targetCard.fragmentEffects = new List<ICardEffect>(payloadEffects);
        targetCard.fragmentDescription = description;
        battleManager.handManager?.RefreshCardDisplay(targetCard);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
