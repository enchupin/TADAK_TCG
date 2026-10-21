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
        // 최대 에너지가 줄어도 이미 얻은 에너지는 유지 (턴 시작 시 최대 에너지로 다시 채움)
        player.energy = Mathf.Max(0, player.energy);
    }
}
