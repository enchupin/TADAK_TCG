using static BattleRuntimeDefinitions;

public sealed class PotionEnhanceBuffScript : PlayerBuffScript
{
    public override int BuffId => PotionEnhanceBuffId;

    public override int ResolvePersistentUpgradeCardId(TrainingBattleManager battleManager, PlayerData player, int cardId, int currentCardId, int sourceBuffId, int stack)
    {
        if (stack <= 0)
        {
            return currentCardId;
        }

        if (sourceBuffId > 0 && sourceBuffId != BuffId)
        {
            return currentCardId;
        }

        // 포션 기본형(x0)은 같은 그룹의 강화형(x1)으로
        return BuffCardUtility.IsPotionCardId(currentCardId) && currentCardId % 10 == 0
            ? currentCardId + 1
            : currentCardId;
    }
}
