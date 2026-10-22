using static BattleRuntimeDefinitions;

// 3035 각인 반복: 다음에 사용하는 각인 카드 {stack}장이 각각 반복을 얻음
public sealed class RepeatNextPowerCardBuffScript : PlayerBuffScript
{
    public override int BuffId => RepeatNextPowerCardBuffId;

    public override int ConsumeRepeatCount(TrainingBattleManager battleManager, PlayerData player, Card playedCard, bool isRepeatedEffect, int stack)
    {
        if (player == null || playedCard == null || isRepeatedEffect || stack <= 0 || !playedCard.HasKeyword(CardKeywordIds.Power))
        {
            return 0;
        }

        // 사용한 카드가 새로 부여하는 중첩과 구분하기 위해 효과 실행 전에 기존 중첩을 소모
        player.ConsumeBuffStack(BuffId, 1);
        return 1;
    }
}
