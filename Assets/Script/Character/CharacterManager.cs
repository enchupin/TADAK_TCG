using System.Collections.Generic;
using UnityEngine;

public static class CharacterManager
{
    private static readonly int[] StarterDeckOffsets = { 10, 20, 30, 40, 50, 60, 70 };
    private static readonly Dictionary<int, string> defaultCharacterNameById = new Dictionary<int, string>();
    private static readonly Dictionary<int, CharacterLocalizationJsonEntry> localizedCharacterNameById = new Dictionary<int, CharacterLocalizationJsonEntry>();

    private static Dictionary<int, CharacterData> characterCache;
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        characterCache = new Dictionary<int, CharacterData>();
        defaultCharacterNameById.Clear();
        localizedCharacterNameById.Clear();

        if (!CharacterJsonParser.TryLoad(out List<CharacterJsonEntry> characters))
        {
            isInitialized = true;
            return;
        }

        BuffMetadataDatabase.Preload();
        BattleRuntimeDefinitions.Initialize();
        LoadCharacterLocalizations();

        foreach (CharacterJsonEntry sourceCharacter in characters)
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
                defaultCharacterNameById[runtimeCharacter.characterId] = runtimeCharacter.characterName;
            }
            else
            {
                Debug.LogWarning($"[CharacterManager] Duplicate characterId found: {runtimeCharacter.characterId}");
            }
        }

        ApplyCurrentLanguage();
        isInitialized = true;

        Debug.Log($"[CharacterManager] 초기화 완료: 총 {characterCache.Count}명의 캐릭터 로드");
    }

    public static void RefreshLocalizedText()
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        ApplyCurrentLanguage();
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

    private static void LoadCharacterLocalizations()
    {
        if (!CharacterLocalizationJsonParser.TryLoad(out List<CharacterLocalizationJsonEntry> localizedCharacters))
        {
            return;
        }

        foreach (CharacterLocalizationJsonEntry localizedCharacter in localizedCharacters)
        {
            if (localizedCharacter == null || localizedCharacter.characterId <= 0)
            {
                continue;
            }

            if (localizedCharacterNameById.ContainsKey(localizedCharacter.characterId))
            {
                Debug.LogWarning($"[CharacterManager] 중복된 캐릭터 로컬라이징 ID가 있습니다: {localizedCharacter.characterId}");
                continue;
            }

            localizedCharacterNameById[localizedCharacter.characterId] = localizedCharacter;
        }
    }

    private static void ApplyCurrentLanguage()
    {
        if (characterCache == null || characterCache.Count == 0)
        {
            return;
        }

        LocalizationLanguage currentLanguage = LocalizationManager.GetCurrentLanguage();
        foreach (KeyValuePair<int, CharacterData> entry in characterCache)
        {
            if (entry.Value == null)
            {
                continue;
            }

            entry.Value.characterName = ResolveLocalizedCharacterName(currentLanguage, entry.Key, entry.Value.characterName);
        }
    }

    private static string ResolveLocalizedCharacterName(LocalizationLanguage currentLanguage, int characterId, string fallbackText)
    {
        string defaultCharacterName = fallbackText ?? string.Empty;
        if (defaultCharacterNameById.TryGetValue(characterId, out string cachedDefaultCharacterName))
        {
            defaultCharacterName = cachedDefaultCharacterName ?? string.Empty;
        }

        if (!localizedCharacterNameById.TryGetValue(characterId, out CharacterLocalizationJsonEntry localizedCharacter) || localizedCharacter == null)
        {
            return defaultCharacterName;
        }

        return LocalizationManager.ResolveLocalizedText(
            currentLanguage,
            localizedCharacter.koName,
            localizedCharacter.enName,
            localizedCharacter.jaName,
            localizedCharacter.zhHantName,
            localizedCharacter.zhHansName,
            localizedCharacter.ruName,
            defaultCharacterName);
    }

    private static bool IsStarterDeckSupported(int characterId)
    {
        return characterId > 0 && characterId != (int)Character.Monster;
    }
}
