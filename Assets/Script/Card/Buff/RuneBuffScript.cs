using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class RuneBuffScript : PlayerBuffScript
{
    public override int BuffId => RuneBuffId;

    public override bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        if (player == null || amount <= 0)
        {
            return false;
        }

        int finalAmount = amount + Mathf.Max(0, player.GetBuffStack(OverchargeBuffId));
        if (finalAmount > 0)
        {
            player.AddBuff(BuffId, finalAmount);
            WuppiModeRuntimeUtility.HandleDirectBuffStackChange(battleManager, player, BuffId);
        }

        return true;
    }
}
