using System.Collections.Generic;
using static BattleRuntimeDefinitions;

public sealed class RepeatNextPowerCardBuffScript : PlayerBuffScript
{
    public override int BuffId => RepeatNextPowerCardBuffId;

    public override void OnCardPlayStarted(TrainingBattleManager battleManager, PlayerData player, Card playedCard, int stack)
    {
        if (battleManager?.handManager == null || player == null || playedCard == null || stack <= 0
            || !playedCard.HasKeyword(CardKeywordIds.Power) || !battleManager.CanGainCardsToHand())
        {
            return;
        }

        // 사용한 카드가 새로 부여하는 중첩과 구분하기 위해 효과 실행 전에 기존 중첩을 소모
        player.ConsumeBuffStack(BuffId, stack);
        List<Card> copies = new();
        for (int i = 0; i < stack; i++)
        {
            Card copy = playedCard.CloneForRuntimeCopy();
            if (copy != null) copies.Add(copy);
        }

        List<Card> generatedCards = battleManager.ProcessGeneratedCards(copies, true);
        if (generatedCards.Count > 0)
        {
            battleManager.handManager.AddCard(generatedCards);
            battleManager.UpdateAllUI();
        }
    }
}
