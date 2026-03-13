using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuffData
{
    public int buffId;
    public string name;
    public int buffType; // 1: Buff, 2: Debuff (Example)
    public string description;
    // We can add triggers and effects parsing later or store them as raw data for now

    public bool IsBeneficialEffect()
    {
        return IsBeneficialBuffId(buffId);
    }

    public bool MatchesAllowedType(int allowedType)
    {
        return IsBeneficialEffect() == IsBeneficialBuffType(allowedType);
    }

    public static int GetPolarityBuffType(int targetBuffId)
    {
        return IsBeneficialBuffId(targetBuffId) ? 1 : 2;
    }

    public static bool IsBeneficialBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit % 2 == 1;
    }

    public static bool IsBeneficialBuffType(int targetBuffType)
    {
        return Mathf.Abs(targetBuffType) % 2 == 1;
    }

    private static int GetLeadingDigit(int value)
    {
        value = Mathf.Abs(value);
        while (value >= 10)
        {
            value /= 10;
        }

        return Math.Max(0, value);
    }
}

[System.Serializable]
public class BuffList
{
    public List<BuffData> buffs;
}

public class Buff
{
    public BuffData data;
    public int stack;
    public int duration;

    public Buff(BuffData data, int stack, int duration)
    {
        this.data = data;
        this.stack = stack;
        this.duration = duration;
    }
}
