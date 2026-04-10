using static BattleRuntimeDefinitions;

public sealed class FeatherEnhanceBuffScript : PlayerBuffScript
{
    private const int BaseFeatherCardId = 203080;
    private const int EnhancedFeatherCardId = 203081;

    public override int BuffId => FeatherEnhanceBuffId;

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

        return currentCardId == BaseFeatherCardId ? EnhancedFeatherCardId : currentCardId;
    }
}
