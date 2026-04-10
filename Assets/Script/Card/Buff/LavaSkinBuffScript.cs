using static BattleRuntimeDefinitions;

public sealed class LavaSkinBuffScript : PlayerBuffScript
{
    public override int BuffId => LavaSkinBuffId;

    public override void OnPlayerHit(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int blockedDamage, int hpDamage, int stack)
    {
        if (battleManager == null || attacker == null || attacker.IsDead() || stack <= 0)
        {
            return;
        }

        battleManager.ApplyBuffToMonster(attacker, BurnBuffId, stack);
    }
}
