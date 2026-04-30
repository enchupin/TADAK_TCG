using static BattleRuntimeDefinitions;

public sealed class DuplicateGenerateBuffScript : PlayerBuffScript
{
    public override int BuffId => DuplicateGenerateBuffId;

    public override int GetGeneratedCardDuplicateCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentDuplicateCount)
    {
        return currentDuplicateCount + stack;
    }
}
