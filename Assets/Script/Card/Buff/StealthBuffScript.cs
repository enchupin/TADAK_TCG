using static BattleRuntimeDefinitions;

// 3065 은신: 받는 피해 25% 감소, 피격될 때마다 1 감소 (R-75)
public sealed class StealthBuffScript : PlayerBuffScript
{
    private const float IncomingDamageMultiplier = 0.75f;

    public override int BuffId => StealthBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, float currentMultiplier)
    {
        return stack > 0 ? currentMultiplier * IncomingDamageMultiplier : currentMultiplier;
    }

    public override bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        if (player == null || stack <= 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        return true;
    }
}
