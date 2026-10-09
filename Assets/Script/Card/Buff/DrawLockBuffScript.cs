using static BattleRuntimeDefinitions;

public sealed class DrawLockBuffScript : PlayerBuffScript
{
    public override int BuffId => DrawLockBuffId;

    public override bool CanDrawCards(TrainingBattleManager battleManager, PlayerData player, int stack, bool currentCanDraw)
    {
        return stack > 0 ? false : currentCanDraw;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.RemoveBuffStack(BuffId);
    }

}
