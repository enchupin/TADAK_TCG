using static BattleRuntimeDefinitions;

public sealed class RuneGenerationBuffScript : PlayerBuffScript
{
    public override int BuffId => RuneGenerationBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(RuneBuffId, stack);
    }
}
