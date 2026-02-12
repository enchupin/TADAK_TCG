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
