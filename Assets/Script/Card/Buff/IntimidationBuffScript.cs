using static BattleRuntimeDefinitions;

public sealed class IntimidationBuffScript : PlayerBuffScript
{
    public override int BuffId => IntimidationBuffId;

    public override void OnPlayerAttackResolved(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int barrierBefore, int barrierAfter, int stack)
    {
        if (battleManager == null || stack <= 0 || barrierBefore <= 0 || barrierAfter > 0)
        {
            return;
        }

        battleManager.ApplyBuffToPlayer(StrengthBuffId, stack);
    }
}
