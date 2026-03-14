using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KeywordData
{
    public int keywordId;
    public string name;
    public string description;
}

[System.Serializable]
public class KeywordList
{
    public List<KeywordData> keywords;
}

public static class KeywordDatabase
{
    private static bool isLoaded;
    private static readonly Dictionary<int, KeywordData> keywordById = new();
    private static readonly Dictionary<string, int> keywordIdByName = new(StringComparer.OrdinalIgnoreCase);

    public static string GetKeywordName(int keywordId)
    {
        EnsureLoaded();
        return keywordById.TryGetValue(keywordId, out KeywordData keywordData) && !string.IsNullOrWhiteSpace(keywordData?.name)
            ? keywordData.name
            : string.Empty;
    }

    public static bool TryGetKeywordId(string keywordName, out int keywordId)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(keywordName))
        {
            keywordId = 0;
            return false;
        }

        return keywordIdByName.TryGetValue(keywordName.Trim(), out keywordId);
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        keywordById.Clear();
        keywordIdByName.Clear();

        TextAsset jsonFile = Resources.Load<TextAsset>("JsonData/keywords");
        if (jsonFile == null)
        {
            Debug.LogWarning("[KeywordDatabase] JsonData/keywords.json을 불러오지 못했습니다");
            return;
        }

        KeywordList keywordList = JsonUtility.FromJson<KeywordList>(jsonFile.text);
        if (keywordList?.keywords == null)
        {
            Debug.LogWarning("[KeywordDatabase] keywords.json 형식이 올바르지 않습니다");
            return;
        }

        foreach (KeywordData keywordData in keywordList.keywords)
        {
            if (keywordData == null || keywordData.keywordId <= 0)
            {
                continue;
            }

            keywordById[keywordData.keywordId] = keywordData;
            if (!string.IsNullOrWhiteSpace(keywordData.name))
            {
                keywordIdByName[keywordData.name.Trim()] = keywordData.keywordId;
            }
        }
    }
}
