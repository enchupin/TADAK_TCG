using System.Collections.Generic;
using UnityEngine;

public static class CharacterManager
{
    private static readonly int[] StarterDeckOffsets = { 10, 20, 30, 40, 50, 60, 70 };

    [System.Serializable]
    private sealed class CharacterJsonRoot
    {
        public List<CharacterJsonData> characters = new List<CharacterJsonData>();
    }

    [System.Serializable]
    private sealed class CharacterJsonData
    {
        public int characterId = 0;
        public string name = string.Empty;
        public int maxHp = 0;
        public string characterColor = string.Empty;
    }

    private static Dictionary<int, CharacterData> characterCache;
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/characters");
        if (jsonFile == null)
        {
            Debug.LogError("[CharacterManager] characters.json을 불러오지 못했습니다");
            characterCache = new Dictionary<int, CharacterData>();
            isInitialized = true;
            return;
        }

        CharacterJsonRoot root = JsonUtility.FromJson<CharacterJsonRoot>(jsonFile.text);
        if (root?.characters == null || root.characters.Count == 0)
        {
            Debug.LogWarning("[CharacterManager] characters.json이 비어 있습니다");
            characterCache = new Dictionary<int, CharacterData>();
            isInitialized = true;
            return;
        }

        BuffMetadataDatabase.Preload();
        BattleRuntimeDefinitions.Initialize();

        characterCache = new Dictionary<int, CharacterData>();
        foreach (CharacterJsonData sourceCharacter in root.characters)
        {
            if (sourceCharacter == null)
            {
                continue;
            }

            CharacterData runtimeCharacter = ScriptableObject.CreateInstance<CharacterData>();
            runtimeCharacter.characterId = sourceCharacter.characterId;
            runtimeCharacter.characterName = sourceCharacter.name ?? string.Empty;
            runtimeCharacter.maxHp = sourceCharacter.maxHp;
            runtimeCharacter.characterColor = sourceCharacter.characterColor ?? string.Empty;

            if (!characterCache.ContainsKey(runtimeCharacter.characterId))
            {
                characterCache[runtimeCharacter.characterId] = runtimeCharacter;
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] Duplicate characterId found: {runtimeCharacter.characterId}");
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
