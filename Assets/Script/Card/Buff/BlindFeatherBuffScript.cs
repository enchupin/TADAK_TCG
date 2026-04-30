using static BattleRuntimeDefinitions;

public sealed class BlindFeatherBuffScript : PlayerBuffScript
{
    public override int BuffId => BlindFeatherBuffId;

    public override bool ShouldApplyFeatherToAllEnemies(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentShouldApplyToAll)
    {
        return currentShouldApplyToAll || stack > 0;
    }
}
