using static BattleRuntimeDefinitions;

// 3067 만조: 매 턴 시작 시 물결 {stack}
public sealed class HighTideBuffScript : PlayerBuffScript
{
    public override int BuffId => HighTideBuffId;

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (battleManager == null || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(WaveBuffId, stack);
    }
}
