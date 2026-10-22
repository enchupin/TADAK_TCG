using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class JackIdentitySkill : CharacterIdentitySkill
{
    public override bool Execute(TrainingBattleManager battleManager, CharacterIdentityState identityState)
    {
        if (!IsBattleReady(battleManager))
        {
            return false;
        }

        // v1.0: 덱(이번 전투의 손·뽑을 카드·버린 카드)에서 1장을 골라 손으로 복사, 행운 획득
        // R-25 유일 카드는 선택지에 보이지 않음
        List<Card> candidates = new List<Card>();
        AddCopyCandidates(candidates, battleManager.handManager?.GetHandCards());
        AddCopyCandidates(candidates, battleManager.usableDeckManager?.GetDrawPile());
        AddCopyCandidates(candidates, battleManager.usableDeckManager?.GetDiscardPile());
        battleManager.ApplyBuffToPlayer(LuckBuffId, 1);
        if (candidates.Count == 0)
        {
            return true;
        }

        void CopySelected(List<Card> selected)
        {
            Card source = selected != null && selected.Count > 0 ? selected[0] : candidates[0];
            Card copy = source.CloneForRuntimeCopy();
            if (copy != null)
            {
                AddGeneratedCardsToHand(battleManager, new List<Card> { copy });
                battleManager.RefreshHandPlayableState();
                battleManager.UpdateAllUI();
            }
        }

        if (!battleManager.OpenSelectCardPanel(candidates, 1, CopySelected))
        {
            CopySelected(null);
        }

        return true;
    }

    private static void AddCopyCandidates(List<Card> candidates, List<Card> source)
    {
        if (source == null)
        {
            return;
        }

        foreach (Card card in source)
        {
            if (card != null && !card.HasKeyword(CardKeywordIds.Unique) && !candidates.Contains(card))
            {
                candidates.Add(card);
            }
        }
    }
}
