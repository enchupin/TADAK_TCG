using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class CorrosionBuffScript : PlayerBuffScript
{
    public override int BuffId => CorrosionBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, float currentMultiplier)
    {
        if (player == null || stack <= 0 || player.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return currentMultiplier;
        }

        bool attackerEnhancesCorrosion = attacker != null && attacker.GetBuffStack(CorrosionEnhanceBuffId) > 0;
        float multiplier = attackerEnhancesCorrosion
            ? BuffValueUtility.GetIncomingDamageMultiplier(EnhancedCorrosionBuffId)
            : BuffValueUtility.GetIncomingDamageMultiplier(BuffId);

        return currentMultiplier * multiplier;
    }

    public override bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int incomingDamage, int stack)
    {
        if (player == null || stack <= 0 || player.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return false;
        }

        player.ConsumeBuffStack(BuffId, 1);
        return true;
    }
}
