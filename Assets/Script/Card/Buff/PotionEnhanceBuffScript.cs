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

        return currentCardId switch
        {
            101080 => 101081,
            101082 => 101083,
            101084 => 101085,
            101086 => 101087,
            _ => currentCardId
        };
    }
}
