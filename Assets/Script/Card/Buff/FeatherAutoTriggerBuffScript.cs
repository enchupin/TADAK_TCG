using static BattleRuntimeDefinitions;

public sealed class FeatherAutoTriggerBuffScript : PlayerBuffScript
{
    public override int BuffId => FeatherAutoTriggerBuffId;

    public override int GetFeatherAutoTriggerCount(TrainingBattleManager battleManager, PlayerData player, int stack, int currentCount)
    {
        return currentCount + stack;
    }
}
