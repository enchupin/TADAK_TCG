using System.Collections.Generic;
using UnityEngine;

public static class CharacterManager
{
    private static readonly int[] StarterDeckOffsets = { 10, 20, 30, 40, 50, 60, 70 };
    private static readonly int[] UniqueCardOffsets = { 30, 40, 50, 60, 70 };
    private static readonly Dictionary<int, string> defaultCharacterNameById = new Dictionary<int, string>();
    // characters.json의 한국어 아이덴티티 설명 (번역이 없을 때 사용)
    private static readonly Dictionary<int, string> defaultIdentityDescriptionById = new Dictionary<int, string>();
    // characters.json에 정의된 시작 덱 (없으면 기존 오프셋 규칙 사용)
    private static readonly Dictionary<int, List<int>> starterDeckById = new Dictionary<int, List<int>>();

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
        defaultIdentityDescriptionById.Clear();
        starterDeckById.Clear();

        if (!CharacterJsonParser.TryLoad(out List<CharacterJsonEntry> characters))
        {
            return;
        }

        BuffMetadataDatabase.Preload();
        BattleRuntimeDefinitions.Initialize();

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
            ApplyIdentityData(runtimeCharacter, sourceCharacter.identityCost);
            runtimeCharacter.IdentitySprite = Resources.Load<Sprite>(
                $"Image/Character/CharacterIdentity/Identity_{(Character)sourceCharacter.characterId}");

            if (!characterCache.ContainsKey(runtimeCharacter.characterId))
            {
                characterCache[runtimeCharacter.characterId] = runtimeCharacter;
                if (sourceCharacter.starterDeck != null && sourceCharacter.starterDeck.Count > 0)
                {
                    starterDeckById[runtimeCharacter.characterId] = new List<int>(sourceCharacter.starterDeck);
                }
                defaultCharacterNameById[runtimeCharacter.characterId] = runtimeCharacter.characterName;
                defaultIdentityDescriptionById[runtimeCharacter.characterId] = sourceCharacter.identityDescription ?? string.Empty;
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

        if (!isInitialized)
        {
            Initialize();
        }

        if (starterDeckById.TryGetValue(characterId, out List<int> dataStarterDeck))
        {
            return new List<int>(dataStarterDeck);
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

    // 고유 카드는 카드 번호 03 ~ 07의 기본형(변형 0)
    public static List<int> GetUniqueBaseCardIds(Character character)
    {
        int characterId = GetIdByCharacterEnum(character);
        List<int> uniqueCardIds = new List<int>(UniqueCardOffsets.Length);
        if (!IsStarterDeckSupported(characterId))
        {
            return uniqueCardIds;
        }

        foreach (int offset in UniqueCardOffsets)
        {
            uniqueCardIds.Add(characterId * 1000 + offset);
        }

        return uniqueCardIds;
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

    private static void ApplyIdentityData(CharacterData runtimeCharacter, int identityCost)
    {
        if (runtimeCharacter == null)
        {
            return;
        }

        runtimeCharacter.identity ??= new CharacterIdentityDefinition();
        runtimeCharacter.identity.description = string.Empty;
        runtimeCharacter.identity.cost = Mathf.Max(0, identityCost);
    }

    private static void ApplyCurrentLanguage()
    {
        if (characterCache == null || characterCache.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<int, CharacterData> entry in characterCache)
        {
            if (entry.Value == null)
            {
                continue;
            }

            defaultCharacterNameById.TryGetValue(entry.Key, out string defaultCharacterName);
            defaultIdentityDescriptionById.TryGetValue(entry.Key, out string defaultIdentityDescription);
            entry.Value.characterName = GameText.CharacterName(entry.Key, defaultCharacterName ?? entry.Value.characterName);
            // 아이덴티티 설명은 [buff:id] 참조를 현재 언어 이름으로 풀어 둠
            entry.Value.identity.description = GameText.FormatPlain(GameText.IdentityDescription(entry.Key, defaultIdentityDescription));
        }
    }

    private static bool IsStarterDeckSupported(int characterId)
    {
        return characterId > 0 && characterId != (int)Character.Monster;
    }
}
