using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuffData
{
    public int buffId;
    public string name;
    public string description;
    public List<BuffEffectData> effects = new();

    public bool IsBeneficialEffect()
    {
        return IsBeneficialBuffId(buffId);
    }

    public bool MatchesAllowedType(int allowedType)
    {
        return GetBuffType(buffId) == GetAllowedBuffType(allowedType);
    }

    public static int GetBuffType(int targetBuffId)
    {
        return GetLeadingDigit(targetBuffId);
    }

    public static bool IsBeneficialBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit % 2 == 1;
    }

    public static bool IsNonStackableBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit == 1 || leadingDigit == 2;
    }

    public static bool IsStackableBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit == 3 || leadingDigit == 4;
    }

    private static int GetAllowedBuffType(int allowedType)
    {
        int normalizedValue = Mathf.Abs(allowedType);
        if (normalizedValue < 10)
        {
            return normalizedValue;
        }

        return GetLeadingDigit(normalizedValue);
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

    public float GetIncomingDamageMultiplier(float fallbackValue = 1f)
    {
        return TryGetMultiplier("EventValue", null, out float multiplier)
            ? multiplier
            : fallbackValue;
    }

    public float GetOutgoingDamageMultiplier(float fallbackValue = 1f)
    {
        return TryGetMultiplier("Damage", "Damage", out float multiplier)
            ? multiplier
            : fallbackValue;
    }

    private bool TryGetMultiplier(string statName, string targetName, out float multiplier)
    {
        multiplier = 1f;
        if (effects == null || effects.Count == 0)
        {
            return false;
        }

        foreach (BuffEffectData effect in effects)
        {
            if (effect == null)
            {
                continue;
            }

            bool isMultiplyEffect =
                string.Equals(effect.change, "Multiply", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(effect.type, "Multiply", StringComparison.OrdinalIgnoreCase);
            if (!isMultiplyEffect || effect.amount <= 0f)
            {
                continue;
            }

            bool statMatches = !string.IsNullOrWhiteSpace(statName) &&
                string.Equals(effect.stat, statName, StringComparison.OrdinalIgnoreCase);
            bool targetMatches = !string.IsNullOrWhiteSpace(targetName) &&
                string.Equals(effect.target, targetName, StringComparison.OrdinalIgnoreCase);

            if (!statMatches && !targetMatches)
            {
                continue;
            }

            multiplier = effect.amount;
            return true;
        }

        return false;
    }
}

[System.Serializable]
public class BuffEffectData
{
    public string type;
    public string stat;
    public string change;
    public string target;
    public float amount;
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
