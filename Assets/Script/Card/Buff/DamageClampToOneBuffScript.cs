using static BattleRuntimeDefinitions;

public sealed class DamageClampToOneBuffScript : PlayerBuffScript
{
    public override int BuffId => DamageClampToOneBuffId;

    public override int ClampIncomingDamage(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, int currentDamage)
    {
        if (stack <= 0 || currentDamage <= 0)
        {
            return currentDamage;
        }

        return 1;
    }

    // 적 턴 동안 받는 피해도 막아야 하므로 적 턴 종료 시 감소
    public override void OnEnemyTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
