using UnityEngine;

public sealed class CharacterIdentityState
{
    public Character Character { get; }
    public CharacterData CharacterData { get; }
    public CharacterIdentitySkill Skill { get; }
    public int CurrentGauge { get; private set; }
    public int MaxGauge => Mathf.Max(0, CharacterData?.identity?.cost ?? 0);
    public string Description => CharacterData?.identity?.description ?? string.Empty;

    public bool CanUse => Skill != null && MaxGauge > 0 && CurrentGauge >= MaxGauge;

    public CharacterIdentityState(Character character, CharacterData characterData, CharacterIdentitySkill skill)
    {
        Character = character;
        CharacterData = characterData;
        Skill = skill;
        CurrentGauge = 0;
    }

    public bool AddGauge(int amount)
    {
        if (amount <= 0 || MaxGauge <= 0)
        {
            return false;
        }

        int nextGauge = Mathf.Clamp(CurrentGauge + amount, 0, MaxGauge);
        if (nextGauge == CurrentGauge)
        {
            return false;
        }

        CurrentGauge = nextGauge;
        return true;
    }

    public bool ConsumeGauge()
    {
        if (!CanUse)
        {
            return false;
        }

        CurrentGauge = Mathf.Max(0, CurrentGauge - MaxGauge);
        return true;
    }
}
