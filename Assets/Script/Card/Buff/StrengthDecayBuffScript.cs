using static BattleRuntimeDefinitions;

public sealed class StrengthDecayBuffScript : PlayerBuffScript
{
    public override int BuffId => StrengthDecayBuffId;

    public override bool AllowsNegativeStack => true;

    // 이중 동작 등으로 반복될 수 있는 턴 종료 효과 (E-28), 음수 중첩이면 힘이 증가
    public override void OnPlayerTurnEndTriggered(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack == 0)
        {
            return;
        }

        player.AddBuff(StrengthBuffId, -stack);
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
