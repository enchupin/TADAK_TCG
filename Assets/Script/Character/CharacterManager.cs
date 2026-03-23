using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 데이터를 관리하는 Static 클래스
/// CharacterData SO들을 로드하여 Dictionary로 캐싱합니다.
/// </summary>
public static class CharacterManager
{
    private static Dictionary<int, CharacterData> characterCache;
    private static bool isInitialized = false;
    
    /// <summary>
    /// 게임 시작 시 자동으로 초기화
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        // Resources 폴더에서 CharacterCollection 로드
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
        
        // Dictionary 캐싱 (characterId로 조회)
        characterCache = new Dictionary<int, CharacterData>();
        foreach (var character in collection.allCharacters)
        {
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
    
    /// <summary>
    /// 캐릭터 ID로 CharacterData 조회
    /// </summary>
    public static CharacterData GetCharacter(int characterId)
    {
        if (!isInitialized) Initialize();
        
        if (characterCache.ContainsKey(characterId))
        {
            return characterCache[characterId];
        }
        
        Debug.LogWarning($"[CharacterManager] Character not found: {characterId}");
        return null;
    }
    
    /// <summary>
    /// 캐릭터의 시작 덱 카드 ID 리스트 반환
    /// </summary>
    public static List<int> GetStartDeck(int characterId)
    {
        CharacterData character = GetCharacter(characterId);
        
        if (character != null && character.startDeckCardIds != null)
        {
            return new List<int>(character.startDeckCardIds);
        }
        
        return new List<int>();
    }

    /// <summary>
    /// 캐릭터의 시작 덱 카드 ID 리스트 반환
    /// </summary>
    public static List<int> GetStartDeck(Character character) {

        int characterId = GetIdByCharacterEnum(character);
        CharacterData characterData = GetCharacter(characterId);

        if (characterData != null && characterData.startDeckCardIds != null) {
            return new List<int>(characterData.startDeckCardIds);
        }
        return new List<int>();
    }

    /// <summary>
    /// 모든 캐릭터 데이터를 리스트로 반환
    /// </summary>
    public static List<CharacterData> GetAllCharacters()
    {
        if (!isInitialized) Initialize();

        return characterCache != null
            ? new List<CharacterData>(characterCache.Values)
            : new List<CharacterData>();
    }
    
    /// <summary>
    /// 초기화 여부 확인
    /// </summary>
    public static bool IsInitialized()
    {
        return isInitialized;
    }
    

          
    /// <summary>
    /// Chacacter ID가 유효한지 검증
    /// </summary>
    public static Character GetCharacterEnumById(int characterId)
    {
        if (System.Enum.IsDefined(typeof(Character), characterId)) {
            return (Character)characterId;
        }
        else {
            Debug.LogWarning($"[CharacterManager] Unknown characterId: {characterId}, defaulting to Isla");
            return Character.Isla;
        }
    }

    /// <summary>
    /// Character enum을 characterId로 변환
    /// </summary>
    public static int GetIdByCharacterEnum(Character character) {
        return (int)character;
    }


    /// <summary>
    /// Character Enum으로 CharacterData 조회
    /// </summary>
    public static CharacterData GetCharacterByEnum(Character character)
    {
        int id = GetIdByCharacterEnum(character);
        return GetCharacter(id);
    }
}
