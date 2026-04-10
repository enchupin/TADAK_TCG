using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class EnhancedCorrosionBuffScript : PlayerBuffScript
{
    public override int BuffId => EnhancedCorrosionBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, Monster attacker, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetIncomingDamageMultiplier(BuffId);

        return Mathf.Max(currentMultiplier, multiplier);
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
