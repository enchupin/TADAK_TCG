public sealed class BondDecayBuffScript : PlayerBuffScript
{
    private const int BuffIdValue = 4015;
    private const int BondBuffId = 3012;

    public override int BuffId => BuffIdValue;

    // 이중 동작 등으로 반복될 수 있는 턴 종료 효과 (E-28)
    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BondBuffId, stack);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
