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

    // 보유하는 동안 모든 피해를 증폭하고 보유자의 턴 시작 시 1 감소 (R-85)
    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0 || player.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
