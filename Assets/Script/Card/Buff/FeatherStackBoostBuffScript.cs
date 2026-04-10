using static BattleRuntimeDefinitions;

public sealed class FeatherStackBoostBuffScript : PlayerBuffScript
{
    public override int BuffId => FeatherStackBoostBuffId;

    public override int ModifyFeatherApplyAmount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentAmount)
    {
        return currentAmount + stack;
    }
}
