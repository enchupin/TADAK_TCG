using System.Collections.Generic;
using UnityEngine;

public static class CharacterManager
{
    private static readonly int[] StarterDeckOffsets = { 10, 20, 30, 40, 50, 60, 70 };

    private static Dictionary<int, CharacterData> characterCache;
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        CharacterCollection collection = Resources.Load<CharacterCollection>("CharacterCollection");

        if (collection == null)
        {
            Debug.LogError("[CharacterManager] CharacterCollection not found in Resources folder!");
            Debug.LogError("[CharacterManager] Please create CharacterCollection.asset in Assets/Resources/");
            return;
        }

        if (collection.allCharacters == null || collection.allCharacters.Count == 0)
        {
            Debug.LogWarning("[CharacterManager] CharacterCollection is empty!");
            characterCache = new Dictionary<int, CharacterData>();
            isInitialized = true;
            return;
        }

        characterCache = new Dictionary<int, CharacterData>();
        foreach (CharacterData character in collection.allCharacters)
        {
            if (character == null)
            {
                continue;
            }

            if (!characterCache.ContainsKey(character.characterId))
            {
                characterCache[character.characterId] = character;
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] Duplicate characterId found: {character.characterId}");
            }
        }

        isInitialized = true;

        Debug.Log($"[CharacterManager] 초기화 완료: 총 {characterCache.Count}명의 캐릭터 로드");
    }

    public static CharacterData GetCharacter(int characterId)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (characterCache != null && characterCache.TryGetValue(characterId, out CharacterData character))
        {
            return character;
        }

        Debug.LogWarning($"[CharacterManager] Character not found: {characterId}");
        return null;
    }

    public static List<int> GetStarterCardIds(int characterId)
    {
        if (!IsStarterDeckSupported(characterId))
        {
            return new List<int>();
        }

        List<int> starterCardIds = new List<int>(StarterDeckOffsets.Length);
        foreach (int offset in StarterDeckOffsets)
        {
            starterCardIds.Add(characterId * 1000 + offset);
        }

        return starterCardIds;
    }

    public static List<int> GetStarterCardIds(Character character)
    {
        return GetStarterCardIds(GetIdByCharacterEnum(character));
    }

    public static List<CharacterData> GetAllCharacters()
    {
        if (!isInitialized)
        {
            Initialize();
        }

        return characterCache != null
            ? new List<CharacterData>(characterCache.Values)
            : new List<CharacterData>();
    }

    public static bool IsInitialized()
    {
        return isInitialized;
    }

    public static Character GetCharacterEnumById(int characterId)
    {
        if (System.Enum.IsDefined(typeof(Character), characterId))
        {
            return (Character)characterId;
        }

        Debug.LogWarning($"[CharacterManager] Unknown characterId: {characterId}, defaulting to Isla");
        return Character.Isla;
    }

    public static int GetIdByCharacterEnum(Character character)
    {
        return (int)character;
    }

    public static CharacterData GetCharacterByEnum(Character character)
    {
        return GetCharacter(GetIdByCharacterEnum(character));
    }

    private static bool IsStarterDeckSupported(int characterId)
    {
        return characterId > 0 && characterId != (int)Character.Monster;
    }
}
