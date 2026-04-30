using static BattleRuntimeDefinitions;

public sealed class OverheatGrowthBuffScript : PlayerBuffScript
{
    public override int BuffId => OverheatGrowthBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(OverheatBuffId, stack);
    }
}
