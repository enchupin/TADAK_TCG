using static BattleRuntimeDefinitions;

public sealed class PoisonUpgradeMonsterBuffScript : MonsterBuffScript
{
    private const int BasePoisonCardId = 20;
    private const int UpgradedPoisonCardId = 21;

    public override int BuffId => PoisonUpgradeBuffId;

    public override int ResolvePersistentUpgradeCardId(TrainingBattleManager battleManager, Monster monster, int cardId, int currentCardId, int sourceBuffId, int stack)
    {
        if (stack <= 0)
        {
            return currentCardId;
        }

        if (sourceBuffId > 0 && sourceBuffId != BuffId)
        {
            return currentCardId;
        }

        return currentCardId == BasePoisonCardId ? UpgradedPoisonCardId : currentCardId;
    }
}
