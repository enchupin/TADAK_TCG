using static BattleRuntimeDefinitions;

public sealed class DoubleActionBuffScript : PlayerBuffScript
{
    public override int BuffId => DoubleActionBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.AddTurnEndTriggerRepeat(stack);
    }
}
