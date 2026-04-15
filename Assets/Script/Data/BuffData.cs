using System;
using UnityEngine;
using static BattleRuntimeDefinitions;

[System.Serializable]
public class BuffData
{
    public int buffId;
    public string name;
    public string description;
    public string koName;
    public string koDescription;
    public string enName;
    public string enDescription;
    public string jaName;
    public string jaDescription;
    public string zhHantName;
    public string zhHantDescription;
    public string zhHansName;
    public string zhHansDescription;

    public static bool IsBeneficialBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit % 2 == 1;
    }

    public static bool IsNonStackableBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit == 1
            || leadingDigit == 2
            || targetBuffId == FaithfulPrayerBuffId
            || targetBuffId == ParasiticMushroomBuffId
            || targetBuffId == PoisonUpgradeBuffId
            || targetBuffId == RootedBuffId
            || targetBuffId == PoisonousMushroomBuffId;
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

public class Buff
{
    public BuffData data;
    public int stack;

    public Buff(BuffData data, int stack)
    {
        this.data = data;
        this.stack = stack;
    }
}
