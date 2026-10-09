using UnityEngine;

public static class CreamBuffRuntimeUtility
{
    public static int ComboBuffId => BattleRuntimeDefinitions.ComboBuffId;
    public static int EnergyOverflowBuffId => BattleRuntimeDefinitions.EnergyOverflowBuffId;

    public static void SyncEnergyOverflow(PlayerData player)
    {
        if (player == null)
        {
            return;
        }

        int overflowStack = Mathf.Max(0, player.GetBuffStack(EnergyOverflowBuffId));
        int baseMaxEnergy = Mathf.Max(0, player.baseMaxEnergy);
        player.maxEnergy = Mathf.Max(0, baseMaxEnergy + overflowStack);
        player.energy = Mathf.Clamp(player.energy, 0, player.maxEnergy);
    }
}
