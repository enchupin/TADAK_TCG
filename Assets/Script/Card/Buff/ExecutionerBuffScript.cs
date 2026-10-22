using static BattleRuntimeDefinitions;

// 1026 처형자: 깃털을 보유한 적을 처치할 때마다 에너지 1, 카드 1장
public sealed class ExecutionerBuffScript : PlayerBuffScript
{
    private const int EnergyAmount = 1;
    private const int DrawAmount = 1;

    public override int BuffId => ExecutionerBuffId;

    public override void OnEnemyKilled(TrainingBattleManager battleManager, PlayerData player, Monster killedMonster, bool killedByDrowning, int stack)
    {
        if (battleManager == null || player == null || killedMonster == null || stack <= 0
            || killedMonster.GetBuffStack(FeatherBuffId) <= 0)
        {
            return;
        }

        player.AddEnergy(EnergyAmount);
        battleManager.DrawCards(DrawAmount);
    }
}
