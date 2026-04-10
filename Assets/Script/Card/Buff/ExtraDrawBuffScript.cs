using static BattleRuntimeDefinitions;

public sealed class ExtraDrawBuffScript : PlayerBuffScript
{
    public override int BuffId => ExtraDrawBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.AddTurnStartDrawModifier(stack);
    }
}
