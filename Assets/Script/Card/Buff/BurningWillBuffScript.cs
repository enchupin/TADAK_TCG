using static BattleRuntimeDefinitions;

public sealed class BurningWillBuffScript : PlayerBuffScript
{
    private const int HpLossPerTurn = 2;

    public override int BuffId => BurningWillBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.LoseHp(HpLossPerTurn);
        player.AddDefense(stack);
    }
}
