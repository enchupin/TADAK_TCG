using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class WeakBuffScript : PlayerBuffScript
{
    public override int BuffId => WeakBuffId;

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetOutgoingDamageMultiplier(BuffId);
        return currentMultiplier * multiplier;
    }

    public override void OnPlayerTurnEnd(TrainingBattleManager battleManager, PlayerData player, int stack)
    {
        if (player == null || stack <= 0)
        {
            return;
        }

        player.ConsumeBuffStack(BuffId, 1);
    }
}
