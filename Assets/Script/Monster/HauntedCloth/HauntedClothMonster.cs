using System.Collections.Generic;
using UnityEngine;

public class HauntedClothMonster : Monster
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

    private int patternIndex;

    public override int MonsterId => 110;
    protected override string MonsterName => "귀신 들린 천";
    protected override int BaseMaxHp => 72;

    protected override void OnBattleStart()
    {
        AddBuff(BattleRuntimeDefinitions.PranksterGhostBuffId, 5);
        patternIndex = 0;
    }

    protected override void BuildNextAction()
    {
        switch (GetCurrentPatternId())
        {
            case 11001:
                SetIntent("적의 무작위 카드 1장을 훔칩니다. 훔친 카드는 '도둑' 버프에 귀속되며, 사망 시 덱에 다시 추가됩니다.");
                SetPlannedPattern(11001, MonsterIntentIconType.HarmfulEffect);
                break;
            case 11002:
                int attackDamage = PreviewOutgoingDamage(11);
                int barrierGain = PreviewBarrierGain(8);
                SetAttackIntent(attackDamage, $"피해를 {attackDamage} 입힙니다. 보호막을 {barrierGain} 얻습니다.");
                SetPlannedPattern(11002, MonsterIntentIconType.Attack, MonsterIntentIconType.Protection);
                break;
            default:
                SetIntent("도망갑니다.");
                SetPlannedPattern(11003, MonsterIntentIconType.Leave);
                break;
        }
    }

    protected override void ExecuteAction(PlayerData target)
    {
        switch (GetCurrentPatternId())
        {
            case 11001:
                StealRandomPlayerCard();
                patternIndex = 1;
                break;
            case 11002:
                DealDamage(target, 11);
                AddDefense(8);
                patternIndex = 0;
                break;
            default:
                LeaveCombat();
                break;
        }
    }

    private int GetCurrentPatternId()
    {
        if (GetBuffStack(BattleRuntimeDefinitions.PranksterGhostBuffId) <= 0)
        {
            return 11003;
        }

        return patternIndex == 0 ? 11001 : 11002;
    }

    private void StealRandomPlayerCard()
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

        StolenCardCandidate candidate = candidates[Random.Range(0, candidates.Count)];
        if (!RemoveCardFromZone(candidate, battleManager))
        {
            return;
        }

        ThiefMonsterBuffScript.RegisterStolenCard(this, candidate.Card);
        AddBuff(BattleRuntimeDefinitions.ThiefBuffId, 1);
        battleManager.UpdateAllUI();
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
