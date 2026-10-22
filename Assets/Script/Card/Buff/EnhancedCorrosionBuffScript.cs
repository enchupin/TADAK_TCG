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

        return currentMultiplier * multiplier;
    }

    // 관통도 부식과 같은 규칙으로 보유자의 턴 시작 시 1 감소 (R-85)
    public override void OnPlayerTurnStart(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
