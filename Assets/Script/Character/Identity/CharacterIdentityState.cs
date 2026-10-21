using UnityEngine;

public sealed class CharacterIdentityState
{
    public Character Character { get; }
    public CharacterData CharacterData { get; }
    public CharacterIdentitySkill Skill { get; }
    public int CurrentGauge { get; private set; }
    public int MaxGauge => Mathf.Max(0, CharacterData?.identity?.cost ?? 0);
    public string Description => CharacterData?.identity?.description ?? string.Empty;
    // 이번 전투에서 스킬을 사용한 횟수와 카드 획득 보상 지급 여부
    public int UsesThisBattle { get; private set; }
    public bool HasReceivedCardRewardThisBattle { get; private set; }

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

    // 훈련 진행 중 이전 전투의 충전 진행도를 복원할 때 사용
    public void SetGauge(int amount)
    {
        CurrentGauge = Mathf.Clamp(amount, 0, MaxGauge);
    }

    public bool ConsumeGauge()
    {
        if (!CanUse)
        {
            return false;
        }

        // 사용 후 충전 진행도는 0으로 초기화
        CurrentGauge = 0;
        UsesThisBattle++;
        return true;
    }

    public void MarkCardRewardReceived()
    {
        HasReceivedCardRewardThisBattle = true;
    }
}
