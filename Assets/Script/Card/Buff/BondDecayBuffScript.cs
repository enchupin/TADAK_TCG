public sealed class BondDecayBuffScript : PlayerBuffScript
{
    private const int BuffIdValue = 3054;
    private const int BondBuffId = 3012;

    public override int BuffId => BuffIdValue;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BondBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
