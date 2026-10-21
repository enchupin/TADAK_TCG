using static BattleRuntimeDefinitions;

// 3073 덫 설치: 적이 공격 의도를 실행할 때마다 1 소모해 그 적에게 피해를 주고 덫 상태로 만듦 (R-77)
public sealed class TrapSetBuffScript : PlayerBuffScript
{
    private const int TrapDamage = 6;

    public override int BuffId => TrapSetBuffId;

    public override void OnEnemyAttackActionStarted(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack)
    {
        if (battleManager == null || player == null || attacker == null || attacker.IsDead() || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
        int damage = battleManager.ResolvePlayerEffectDamage(TrapDamage);
        if (damage > 0)
        {
            int dealtDamage = attacker.TakeDamage(damage, 0);
            battleManager.Buffs.Player.OnPlayerDamageDealt(attacker, dealtDamage);
        }

        if (!attacker.IsDead())
        {
            battleManager.ApplyBuffToMonster(attacker, TrapStateBuffId, 1);
        }
    }
}
