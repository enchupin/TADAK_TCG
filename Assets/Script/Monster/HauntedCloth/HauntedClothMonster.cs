using System.Collections.Generic;
using UnityEngine;

// v1.0 110 귀신 들린 천
// 체력·패턴·AI는 monsters.json 데이터 사용 (DataMonster), 장난꾸러기 유령이 0이 되면 P03 이탈 우선
// P01 카드 훔치기: 위치 무관, 강화된 카드 우선 → 각인 카드 우선 → 같으면 무작위 (E-44)
public class HauntedClothMonster : DataMonster
{
    private enum PlayerCardZone
    {
        Hand,
        DrawPile,
        DiscardPile
    }

    private readonly struct StolenCardCandidate
    {
        public StolenCardCandidate(Card card, PlayerCardZone zone)
        {
            Card = card;
            Zone = zone;
        }

        public Card Card { get; }
        public PlayerCardZone Zone { get; }
    }

    protected override IEnumerable<MonsterIntentIconType> GetSpecialIcons(string key)
    {
        yield return MonsterIntentIconType.DisruptCard;
    }

    protected override void ExecuteSpecial(string key, MonsterActionDefinition action, PlayerData target)
    {
        if (key == "steal_card")
        {
            StealPlayerCard();
            return;
        }

        base.ExecuteSpecial(key, action, target);
    }

    private void StealPlayerCard()
    {
        TrainingBattleManager battleManager = TrainingBattleManager.Instance;
        if (battleManager == null || battleManager.handManager == null || battleManager.usableDeckManager == null)
        {
            return;
        }

        List<StolenCardCandidate> candidates = new List<StolenCardCandidate>();
        AddCandidates(candidates, battleManager.handManager.GetHandCards(), PlayerCardZone.Hand);
        AddCandidates(candidates, battleManager.usableDeckManager.GetDrawPile(), PlayerCardZone.DrawPile);
        AddCandidates(candidates, battleManager.usableDeckManager.GetDiscardPile(), PlayerCardZone.DiscardPile);

        if (candidates.Count <= 0)
        {
            return;
        }

        StolenCardCandidate candidate = PickPriorityCandidate(candidates);
        if (!RemoveCardFromZone(candidate, battleManager))
        {
            return;
        }

        MonsterBuffs.ThiefBuffScript.RegisterStolenCard(this, candidate.Card);
        AddBuff(BattleRuntimeDefinitions.ThiefBuffId, 1);
        battleManager.UpdateAllUI();
    }

    // 강화된 카드(변형 1~5) 우선, 그다음 각인 카드, 같은 우선순위 안에서는 무작위
    private static StolenCardCandidate PickPriorityCandidate(List<StolenCardCandidate> candidates)
    {
        int bestPriority = int.MinValue;
        List<StolenCardCandidate> best = new List<StolenCardCandidate>();
        foreach (StolenCardCandidate candidate in candidates)
        {
            int priority = (IsUpgradedCard(candidate.Card) ? 2 : 0) + (candidate.Card.HasKeyword(CardKeywordIds.Power) ? 1 : 0);
            if (priority > bestPriority)
            {
                bestPriority = priority;
                best.Clear();
            }

            if (priority == bestPriority)
            {
                best.Add(candidate);
            }
        }

        return best[Random.Range(0, best.Count)];
    }

    private static bool IsUpgradedCard(Card card)
    {
        return card != null && Mathf.Abs(card.cardId) % 10 != 0;
    }

    private static void AddCandidates(List<StolenCardCandidate> candidates, List<Card> cards, PlayerCardZone zone)
    {
        if (candidates == null || cards == null || cards.Count <= 0)
        {
            return;
        }

        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            candidates.Add(new StolenCardCandidate(card, zone));
        }
    }

    private static bool RemoveCardFromZone(StolenCardCandidate candidate, TrainingBattleManager battleManager)
    {
        if (battleManager == null || candidate.Card == null)
        {
            return false;
        }

        switch (candidate.Zone)
        {
            case PlayerCardZone.Hand:
                return battleManager.handManager != null && battleManager.handManager.RemoveCard(candidate.Card);
            case PlayerCardZone.DrawPile:
                return battleManager.usableDeckManager != null && battleManager.usableDeckManager.RemoveFromDrawPile(candidate.Card);
            case PlayerCardZone.DiscardPile:
                if (battleManager.usableDeckManager == null)
                {
                    return false;
                }

                battleManager.usableDeckManager.RemoveFromDiscard(candidate.Card);
                return true;
            default:
                return false;
        }
    }
}
