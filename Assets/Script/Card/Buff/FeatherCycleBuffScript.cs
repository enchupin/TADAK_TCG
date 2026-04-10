using static BattleRuntimeDefinitions;

public sealed class FeatherCycleBuffScript : PlayerBuffScript
{
    public override int BuffId => FeatherCycleBuffId;

    public override void OnFeatherApplied(TrainingBattleManager battleManager, PlayerData player, int appliedAmount, int targetCount, TargetType targetType, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.DrawCards(stack);
    }
}
