using static BattleRuntimeDefinitions;

public sealed class CounterattackBuffScript : PlayerBuffScript
{
    public override int BuffId => CounterattackBuffId;

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (attacker == null || attacker.IsDead() || stack <= 0)
        {
            return;
        }

        attacker.TakeDamage(stack, 0);
    }
}
