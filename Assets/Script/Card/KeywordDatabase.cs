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

    public static string GetKeywordDescription(int keywordId)
    {
        EnsureLoaded();
        return keywordById.TryGetValue(keywordId, out KeywordData keywordData) && !string.IsNullOrWhiteSpace(keywordData?.description)
            ? keywordData.description
            : string.Empty;
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

        if (!KeywordJsonParser.TryLoad(out List<KeywordData> keywords))
        {
            return;
        }

        foreach (KeywordData keywordData in keywords)
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
