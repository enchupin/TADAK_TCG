using System.Collections.Generic;
using UnityEngine;

// 조각: 손의 다른 카드 1장(동료 카드 포함)에 효과를 붙임
// 붙은 카드를 쓸 때 본래 효과 다음에 페이로드 카드의 효과가 발동하며 이번 전투 동안 유지 (R-73)
// 한 카드에는 조각을 1개만 새길 수 있음
public class FragmentEffect : ICardEffect
{
    public int fragmentCardId;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager?.handManager == null || fragmentCardId <= 0)
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

        if (battleManager.OpenSelectCardPanel(candidates, 1, selectedCards => ApplyFragment(battleManager, selectedCards)))
        {
            return;
        }

        ApplyFragment(battleManager, new List<Card> { candidates[0] });
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
            if (handCard != null && handCard != hostCard && handCard.fragmentCardId <= 0)
            {
                candidates.Add(handCard);
            }
        }

        return candidates;
    }

    private void ApplyFragment(TrainingBattleManager battleManager, List<Card> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count == 0 || selectedCards[0] == null || selectedCards[0].fragmentCardId > 0)
        {
            return;
        }

        Card targetCard = selectedCards[0];
        targetCard.fragmentCardId = fragmentCardId;
        battleManager.handManager?.RefreshCardDisplay(targetCard);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
