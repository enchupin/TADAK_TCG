using System.Collections.Generic;
using UnityEngine;

public static class BuffCombatUtility
{
    public static void ReducePlayerMaxHp(PlayerData target, int amount)
    {
        if (target == null || amount <= 0)
        {
            return;
        }

        target.maxHP = Mathf.Max(1, target.maxHP - amount);
        target.hp = Mathf.Min(target.hp, target.maxHP);

        if (TrainingRunState.IsRunActive)
        {
            TrainingRunState.SetPlayerHealthState(target.hp, target.maxHP);
        }

        TrainingBattleManager.Instance?.UpdateAllUI();
    }

    public static void TransferPlayerMaxHpToMonster(PlayerData target, Monster monster, int amount)
    {
        if (target == null || monster == null || amount <= 0)
        {
            return;
        }

        ReducePlayerMaxHp(target, amount);

        monster.maxHP += amount;
        monster.hp = Mathf.Min(monster.maxHP, monster.hp + amount);
        monster.UpdateUI();
        TrainingBattleManager.Instance?.UpdateAllUI();
    }

    public static Buff RemoveRandomBeneficialBuff(Monster monster, int excludedBuffId = 0)
    {
        if (monster?.currentBuffs == null)
        {
            return null;
        }

        List<Buff> candidates = new();
        foreach (Buff buff in monster.currentBuffs)
        {
            if (!IsRemovableBeneficialBuff(buff, excludedBuffId))
            {
                continue;
            }

            candidates.Add(buff);
        }

        if (candidates.Count <= 0)
        {
            return null;
        }

        Buff removedBuff = candidates[Random.Range(0, candidates.Count)];
        monster.currentBuffs.Remove(removedBuff);
        return removedBuff;
    }

    public static Buff RemoveRandomBeneficialBuff(PlayerData player)
    {
        if (player?.currentBuffs == null)
        {
            return null;
        }

        List<Buff> candidates = new();
        foreach (Buff buff in player.currentBuffs)
        {
            if (!IsRemovableBeneficialBuff(buff, 0))
            {
                continue;
            }

            candidates.Add(buff);
        }

        if (candidates.Count <= 0)
        {
            return null;
        }

        Buff removedBuff = candidates[Random.Range(0, candidates.Count)];
        player.currentBuffs.Remove(removedBuff);
        return removedBuff;
    }

    public static int CountDistinctNegativeBuffTypes(Monster monster)
    {
        if (monster?.currentBuffs == null)
        {
            return 0;
        }

        HashSet<int> negativeBuffIds = new();
        foreach (Buff buff in monster.currentBuffs)
        {
            if (buff?.data == null || BuffData.IsBeneficialBuffId(buff.data.buffId))
            {
                continue;
            }

            negativeBuffIds.Add(buff.data.buffId);
        }

        return negativeBuffIds.Count;
    }

    public static void RemoveAllNegativeBuffs(Monster monster)
    {
        if (monster?.currentBuffs == null)
        {
            return;
        }

        monster.currentBuffs.RemoveAll(buff => buff?.data != null && !BuffData.IsBeneficialBuffId(buff.data.buffId));
    }

    private static bool IsRemovableBeneficialBuff(Buff buff, int excludedBuffId)
    {
        return buff?.data != null
            && buff.data.buffId != excludedBuffId
            && BuffData.IsBeneficialBuffId(buff.data.buffId);
    }
}
