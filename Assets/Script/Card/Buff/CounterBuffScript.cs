using static BattleRuntimeDefinitions;

// 3062 반격: 공격을 완전히 막을 때마다(보호막으로 피해 0, 또는 회피) 공격자에게 피해 {stack}, 다음 내 턴 시작 시 제거 (R-74)
public sealed class CounterBuffScript : PlayerBuffScript
{
    public override int BuffId => CounterBuffId;

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (battleManager == null || attacker == null || attacker.IsDead() || hpDamage > 0 || stack <= 0)
        {
            return;
        }

        int damage = battleManager.ResolvePlayerEffectDamage(stack);
        if (damage > 0)
        {
            int dealtDamage = attacker.TakeDamage(damage, 0);
            battleManager.Buffs.Player.OnPlayerDamageDealt(attacker, dealtDamage);
        }
    }

    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        player?.RemoveBuffStack(BuffId);
    }
}
