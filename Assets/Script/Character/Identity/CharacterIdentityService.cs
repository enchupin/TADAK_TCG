using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterIdentityService
{
    private readonly TrainingBattleManager battleManager;
    private readonly Dictionary<Character, CharacterIdentityState> stateByCharacter = new Dictionary<Character, CharacterIdentityState>();
    private readonly List<CharacterIdentityState> states = new List<CharacterIdentityState>();
    private readonly Dictionary<Character, CharacterIdentitySkill> skills;

    public CharacterIdentityService(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
        skills = CharacterIdentitySkillRegistry.Create();
    }

    public void Initialize(IReadOnlyList<Character> selectedCharacters)
    {
        stateByCharacter.Clear();
        states.Clear();

        if (selectedCharacters == null)
        {
            return;
        }

        foreach (Character character in selectedCharacters)
        {
            if (stateByCharacter.ContainsKey(character))
            {
                continue;
            }

            CharacterData characterData = CharacterManager.GetCharacterByEnum(character);
            CharacterIdentitySkill skill = ResolveSkill(character);
            CharacterIdentityState state = new CharacterIdentityState(character, characterData, skill);
            stateByCharacter.Add(character, state);
            states.Add(state);
        }
        states.Sort((left, right) => ((int)left.Character).CompareTo((int)right.Character));
    }

    public IReadOnlyList<CharacterIdentityState> GetStates()
    {
        return states;
    }

    public CharacterIdentityState GetState(Character character)
    {
        stateByCharacter.TryGetValue(character, out CharacterIdentityState state);
        return state;
    }

    public bool AddGauge(Character character, int amount)
    {
        CharacterIdentityState state = GetState(character);
        return state != null && state.AddGauge(amount);
    }

    // 카드 사용 시 충전량을 rules.json의 chargeMode에 따라 계산
    // energy 모드는 그 캐릭터 카드에 실제로 지불한 에너지만 충전하고 보호막·동력 비용은 제외
    public bool ChargeForPlayedCard(Card playedCard, int paidCost)
    {
        if (playedCard == null || playedCard.character == Character.Monster)
        {
            return false;
        }

        return AddGauge(playedCard.character, ResolveChargeAmount(playedCard, paidCost));
    }

    public static int ResolveChargeAmount(Card playedCard, int paidCost)
    {
        if (playedCard == null)
        {
            return 0;
        }

        if (GameRules.Combat.chargeMode == IdentityChargeModes.Card)
        {
            return 1;
        }

        return playedCard.costType == CardCostType.Energy ? Mathf.Max(0, paidCost) : 0;
    }

    public void AddGaugeToOtherCharacters(Character sourceCharacter, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        foreach (CharacterIdentityState state in states)
        {
            if (state == null || state.Character == sourceCharacter)
            {
                continue;
            }

            state.AddGauge(amount);
        }
    }

    public bool CanUse(Character character)
    {
        CharacterIdentityState state = GetState(character);
        return state != null && state.CanUse;
    }

    public bool TryUse(Character character)
    {
        CharacterIdentityState state = GetState(character);
        if (state == null || !state.CanUse || state.Skill == null)
        {
            return false;
        }

        if (!state.Skill.Execute(battleManager, state))
        {
            return false;
        }

        return state.ConsumeGauge();
    }

    // 전투마다 캐릭터별로 usesPerNewCard회 사용했을 때 한 번만 카드 획득 보상을 지급
    public bool TryClaimCardReward(Character character)
    {
        CharacterIdentityState state = GetState(character);
        int requiredUses = Mathf.Max(1, GameRules.Identity.usesPerNewCard);
        if (state == null || state.HasReceivedCardRewardThisBattle || state.UsesThisBattle < requiredUses)
        {
            return false;
        }

        state.MarkCardRewardReceived();
        return true;
    }

    public Dictionary<Character, int> ExportGauges()
    {
        Dictionary<Character, int> gauges = new Dictionary<Character, int>();
        foreach (CharacterIdentityState state in states)
        {
            if (state != null)
            {
                gauges[state.Character] = state.CurrentGauge;
            }
        }

        return gauges;
    }

    public void RestoreGauges(IReadOnlyDictionary<Character, int> gauges)
    {
        if (gauges == null)
        {
            return;
        }

        foreach (CharacterIdentityState state in states)
        {
            if (state != null && gauges.TryGetValue(state.Character, out int gauge))
            {
                state.SetGauge(gauge);
            }
        }
    }

    public int GetGauge(Character character)
    {
        CharacterIdentityState state = GetState(character);
        return state != null ? state.CurrentGauge : 0;
    }

    public int GetCost(Character character)
    {
        CharacterIdentityState state = GetState(character);
        return state != null ? state.MaxGauge : 0;
    }

    public string GetDescription(Character character)
    {
        CharacterIdentityState state = GetState(character);
        return state != null ? state.Description : string.Empty;
    }

    private CharacterIdentitySkill ResolveSkill(Character character)
    {
        if (skills.TryGetValue(character, out CharacterIdentitySkill skill))
        {
            return skill;
        }

        Debug.LogWarning($"[CharacterIdentityService] 아이덴티티 스킬이 없습니다: {character}");
        return null;
    }
}
