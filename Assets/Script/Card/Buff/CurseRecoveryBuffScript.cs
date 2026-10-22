using static BattleRuntimeDefinitions;

// 3075 저주 회복: 보유자의 턴 종료 시 힘 {stack}을 얻고 제거
public sealed class CurseRecoveryBuffScript : PlayerBuffScript
{
    public override int BuffId => CurseRecoveryBuffId;

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.AddBuff(StrengthBuffId, stack);
        player.RemoveBuffStack(BuffId);
    }
}
