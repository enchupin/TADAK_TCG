using static BattleRuntimeDefinitions;

// 3069 물의 품: 익사로 적이 죽을 때마다 물결 {stack}
public sealed class WaterEmbraceBuffScript : PlayerBuffScript
{
    public override int BuffId => WaterEmbraceBuffId;

    public override void OnEnemyKilled(TrainingBattleManager battleManager, PlayerData player, Monster killedMonster, bool killedByDrowning, int stack)
    {
        if (battleManager == null || !killedByDrowning || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(WaveBuffId, stack);
    }
}
