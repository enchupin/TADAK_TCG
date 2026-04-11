using UnityEngine;

public static class CreamBuffRuntimeUtility
{
    public const int EnergyOverflowBuffId = 3048;

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

public sealed class StrengthDecayMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => 4006;

    public override int ModifyOutgoingDamage(TrainingBattleManager battleManager, Monster monster, int stack, int currentDamage)
    {
        if (stack <= 0 || currentDamage <= 0)
        {
            return currentDamage;
        }

        return Mathf.Max(0, currentDamage - stack);
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, stack);
    }
}

public sealed class EnergyOverflowBuffScript : PlayerBuffScript
{
    public override int BuffId => 3048;

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
