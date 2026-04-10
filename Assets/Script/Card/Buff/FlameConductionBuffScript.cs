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
}
