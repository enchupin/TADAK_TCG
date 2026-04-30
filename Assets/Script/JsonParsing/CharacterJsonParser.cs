using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CharacterJsonEntry
{
    public int characterId = 0;
    public string name = string.Empty;
    public int maxHp = 0;
    public int identityCost = 0;
}

public static class CharacterJsonParser
{
    private const string CharacterResourcePath = "JsonData/characters";

    public static bool TryLoad(out List<CharacterJsonEntry> characters)
    {
        characters = new List<CharacterJsonEntry>();

        TextAsset jsonFile = Resources.Load<TextAsset>(CharacterResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError("[CharacterJsonParser] characters.json을 불러오지 못했습니다");
            return false;
        }

        CharacterJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<CharacterJsonRoot>(jsonFile.text);
        }
        catch
        {
            Debug.LogError("[CharacterJsonParser] characters.json 파싱에 실패했습니다");
            return false;
        }

        if (root?.characters == null || root.characters.Count == 0)
        {
            Debug.LogWarning("[CharacterJsonParser] characters.json이 비어 있습니다");
            return false;
        }

        characters = root.characters;
        return true;
    }

    [Serializable]
    private sealed class CharacterJsonRoot
    {
        public List<CharacterJsonEntry> characters = new List<CharacterJsonEntry>();
    }
}
