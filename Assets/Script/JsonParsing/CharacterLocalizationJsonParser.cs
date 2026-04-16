using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CharacterLocalizationJsonEntry
{
    public int characterId = 0;
    public string koName = string.Empty;
    public string enName = string.Empty;
    public string jaName = string.Empty;
    public string zhHantName = string.Empty;
    public string zhHansName = string.Empty;
    public string ruName = string.Empty;
}

public static class CharacterLocalizationJsonParser
{
    private const string CharacterLocalizationResourcePath = "Localization/characters";

    public static bool TryLoad(out List<CharacterLocalizationJsonEntry> characters)
    {
        characters = new List<CharacterLocalizationJsonEntry>();

        TextAsset jsonFile = Resources.Load<TextAsset>(CharacterLocalizationResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning("[CharacterLocalizationJsonParser] characters.json을 불러오지 못했습니다");
            return false;
        }

        CharacterLocalizationJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<CharacterLocalizationJsonRoot>(jsonFile.text);
        }
        catch
        {
            Debug.LogWarning("[CharacterLocalizationJsonParser] characters.json 파싱에 실패했습니다");
            return false;
        }

        if (root?.characters == null || root.characters.Count == 0)
        {
            Debug.LogWarning("[CharacterLocalizationJsonParser] characters.json이 비어 있습니다");
            return false;
        }

        characters = root.characters;
        return true;
    }

    [Serializable]
    private sealed class CharacterLocalizationJsonRoot
    {
        public List<CharacterLocalizationJsonEntry> characters = new List<CharacterLocalizationJsonEntry>();
    }
}
