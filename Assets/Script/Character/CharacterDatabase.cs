using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 데이터베이스 - 모든 캐릭터 ScriptableObject를 관리합니다.
/// CardDatabase와 동일한 구조로 작동합니다.
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    private static CharacterDatabase instance;
    public static CharacterDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<CharacterDatabase>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CharacterDatabase");
                    instance = go.AddComponent<CharacterDatabase>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private Dictionary<int, CharacterData> characterDictionary = new Dictionary<int, CharacterData>();
    private bool isLoaded = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllCharacters();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Resources 폴더에서 모든 캐릭터 데이터를 로드합니다.
    /// </summary>
    private void LoadAllCharacters()
    {
        if (isLoaded) return;

        CharacterData[] characters = Resources.LoadAll<CharacterData>("CharacterData");

        if (characters == null || characters.Length == 0)
        {
            Debug.LogWarning("No character data found in Resources/CharacterData folder");
            return;
        }

        characterDictionary.Clear();
        foreach (var character in characters)
        {
            if (!characterDictionary.ContainsKey(character.characterId))
            {
                characterDictionary.Add(character.characterId, character);
            }
            else
            {
                Debug.LogWarning($"Duplicate character ID found: {character.characterId}");
            }
        }

        isLoaded = true;
        Debug.Log($"CharacterDatabase: Loaded {characterDictionary.Count} characters");
    }

    /// <summary>
    /// 특정 ID의 캐릭터 데이터를 반환
    /// </summary>
    public CharacterData GetCharacterById(int characterId)
    {
        if (!isLoaded)
        {
            LoadAllCharacters();
        }

        if (characterDictionary.TryGetValue(characterId, out CharacterData character))
        {
            return character;
        }

        Debug.LogWarning($"Character with ID {characterId} not found");
        return null;
    }

    /// <summary>
    /// 특정 이름의 캐릭터 데이터를 반환
    /// </summary>
    public CharacterData GetCharacterByName(string name)
    {
        if (!isLoaded)
        {
            LoadAllCharacters();
        }

        foreach (var kvp in characterDictionary)
        {
            if (kvp.Value.characterName == name)
            {
                return kvp.Value;
            }
        }

        Debug.LogWarning($"Character with name '{name}' not found");
        return null;
    }

    /// <summary>
    /// 모든 캐릭터 데이터를 반환
    /// </summary>
    public List<CharacterData> GetAllCharacters()
    {
        if (!isLoaded)
        {
            LoadAllCharacters();
        }

        return new List<CharacterData>(characterDictionary.Values);
    }

    /// <summary>
    /// 특정 색상의 캐릭터들을 반환
    /// </summary>
    public List<CharacterData> GetCharactersByColor(string color)
    {
        if (!isLoaded)
        {
            LoadAllCharacters();
        }

        List<CharacterData> result = new List<CharacterData>();
        foreach (var kvp in characterDictionary)
        {
            if (kvp.Value.characterColor == color)
            {
                result.Add(kvp.Value);
            }
        }

        return result;
    }

    /// <summary>
    /// 캐릭터의 시작 덱 카드 ID 리스트를 반환
    /// </summary>
    public List<int> GetStartDeck(int characterId)
    {
        CharacterData character = GetCharacterById(characterId);
        
        if (character != null && character.startDeckCardIds != null)
        {
            return new List<int>(character.startDeckCardIds);
        }

        return new List<int>();
    }

    /// <summary>
    /// 로드된 캐릭터 수를 반환
    /// </summary>
    public int GetCharacterCount()
    {
        if (!isLoaded)
        {
            LoadAllCharacters();
        }

        return characterDictionary.Count;
    }

    /// <summary>
    /// 데이터베이스가 로드되었는지 확인
    /// </summary>
    public bool IsLoaded()
    {
        return isLoaded;
    }

    /// <summary>
    /// 데이터베이스를 강제로 다시 로드
    /// </summary>
    public void ReloadDatabase()
    {
        isLoaded = false;
        LoadAllCharacters();
    }
}
