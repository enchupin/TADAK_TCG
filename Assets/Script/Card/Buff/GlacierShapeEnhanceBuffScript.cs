using static BattleRuntimeDefinitions;

public sealed class GlacierShapeEnhanceBuffScript : PlayerBuffScript
{
    public override int BuffId => GlacierShapeEnhanceBuffId;

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

        return currentCardId == 301080 ? 301081 : currentCardId;
    }
}
