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

        // v1.0 발열은 과열 대신 강공을 얻음
        battleManager.ApplyBuffToPlayer(AttackBoostBuffId, stack);
    }
}
