using UnityEngine;

public sealed class EnergyOverflowBuffScript : PlayerBuffScript
{
    public override int BuffId => CreamBuffRuntimeUtility.EnergyOverflowBuffId;

    public override bool TryApplyToPlayer(TrainingBattleManager battleManager, PlayerData player, int amount)
    {
        if (player == null || amount <= 0)
        {
            return false;
        }

        player.AddBuff(BuffId, amount);
        CreamBuffRuntimeUtility.SyncEnergyOverflow(player);
        battleManager?.UpdateAllUI();
        return true;
    }
}
