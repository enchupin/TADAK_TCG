using static BattleRuntimeDefinitions;

public sealed class ThornBuffScript : PlayerBuffScript
{
    public override int BuffId => ThornBuffId;

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (attacker == null || attacker.IsDead() || stack <= 0)
        {
            return;
        }

        attacker.TakeDamage(stack, 0);
    }
}
