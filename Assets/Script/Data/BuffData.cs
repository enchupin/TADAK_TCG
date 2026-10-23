using System;
using UnityEngine;
using static BattleRuntimeDefinitions;

// 상태 효과 극성: 패시브는 몬스터 고유 효과로 이로운/해로운 효과를 다루는 카드 능력의 대상이 아님
public enum BuffPolarity
{
    Beneficial,
    Harmful,
    Passive
}

[System.Serializable]
public class BuffData
{
    public int buffId;
    // buffs.json의 polarity, stackable, signed 값
    public BuffPolarity polarity;
    public bool stackable = true;
    public bool signed;
    public string name;
    public string description;
    // name · description은 현재 언어 문구, 아래는 JsonData/buffs.json 한국어 원문 (내부 이름 조회용)
    public string sourceName;
    public string sourceDescription;

    // 극성과 중첩 규칙은 buffs.json 메타데이터를 우선 사용하고 없으면 ID 규칙으로 판정
    public static BuffPolarity GetPolarity(int targetBuffId)
    {
        if (BuffMetadataDatabase.TryGetBuffData(targetBuffId, out BuffData data) && data != null)
        {
            return data.polarity;
        }

        int leadingDigit = GetLeadingDigit(targetBuffId);
        if (leadingDigit == 5)
        {
            return BuffPolarity.Passive;
        }

        return leadingDigit % 2 == 1 ? BuffPolarity.Beneficial : BuffPolarity.Harmful;
    }

    public static bool IsBeneficialBuffId(int targetBuffId)
    {
        return GetPolarity(targetBuffId) == BuffPolarity.Beneficial;
    }

    public static bool IsHarmfulBuffId(int targetBuffId)
    {
        return GetPolarity(targetBuffId) == BuffPolarity.Harmful;
    }

    public static bool IsPassiveBuffId(int targetBuffId)
    {
        return GetPolarity(targetBuffId) == BuffPolarity.Passive;
    }

    // 이로운 효과를 훔치거나 제거하는 카드 능력의 대상 (1xxx · 3xxx만, R-82)
    public static bool IsRemovableBeneficialBuffId(int targetBuffId)
    {
        int leadingDigit = GetLeadingDigit(targetBuffId);
        return (leadingDigit == 1 || leadingDigit == 3) && IsBeneficialBuffId(targetBuffId);
    }

    public static bool IsNonStackableBuffId(int targetBuffId)
    {
        if (BuffMetadataDatabase.TryGetBuffData(targetBuffId, out BuffData data) && data != null)
        {
            return !data.stackable;
        }

        int leadingDigit = GetLeadingDigit(targetBuffId);
        return leadingDigit == 1
            || leadingDigit == 2
            || targetBuffId == FaithfulPrayerBuffId
            || targetBuffId == ParasiticMushroomBuffId
            || targetBuffId == PoisonUpgradeBuffId
            || targetBuffId == RootedBuffId
            || targetBuffId == HarmonyBuffId
            || targetBuffId == PoisonousMushroomBuffId;
    }

    // 힘처럼 음수 중첩을 가질 수 있는 효과
    public static bool AllowsNegativeStack(int targetBuffId)
    {
        return BuffMetadataDatabase.TryGetBuffData(targetBuffId, out BuffData data) && data != null && data.signed;
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
