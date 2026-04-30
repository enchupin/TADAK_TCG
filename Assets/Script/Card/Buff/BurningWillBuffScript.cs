using static BattleRuntimeDefinitions;

public sealed class BurningWillBuffScript : PlayerBuffScript
{
    public override int BuffId => BurningWillBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.LoseHp(1);
        player.AddDefense(stack);
    }
}
