using static BattleRuntimeDefinitions;

public sealed class FlameConductionBuffScript : PlayerBuffScript
{
    public override int BuffId => FlameConductionBuffId;

    public override void OnPlayerAttackResolved(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int barrierBefore, int barrierAfter, int stack)
    {
        if (battleManager == null || targetMonster == null || targetMonster.IsDead() || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToMonster(targetMonster, BurnBuffId, stack);
    }

    // 이번 턴 동안만 유지되고 내 턴 종료 시 제거
    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
