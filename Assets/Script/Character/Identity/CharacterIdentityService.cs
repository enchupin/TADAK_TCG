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
