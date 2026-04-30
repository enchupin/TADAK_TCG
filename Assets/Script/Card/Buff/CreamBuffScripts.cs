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

public sealed class StrengthDecayMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => 4006;

    public override int GetOutgoingDamageFlatBonus(TrainingBattleManager battleManager, Monster monster, int stack, int currentBonus)
    {
        if (stack <= 0)
        {
            return currentBonus;
        }

        return currentBonus - stack;
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

public sealed class ComboBuffScript : PlayerBuffScript
{
    private int damageHitCount;

    public override int BuffId => CreamBuffRuntimeUtility.ComboBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        damageHitCount = 0;
    }

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack <= 0 || damageHitCount < 6)
        {
            return currentMultiplier;
        }

        return currentMultiplier * 2f;
    }

    public override void OnPlayerDamageDealt(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int dealtDamage, int stack)
    {
        if (stack <= 0 || dealtDamage <= 0)
        {
            return;
        }

        if (damageHitCount >= 6)
        {
            damageHitCount = 0;
            return;
        }

        damageHitCount++;
    }
}

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
