using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KeywordData
{
    public int keywordId;
    public string name;
    public string description;
    public string enName;
    public string enDescription;
    public string jaName;
    public string jaDescription;
    public string zhHantName;
    public string zhHantDescription;
    public string zhHansName;
    public string zhHansDescription;
    public string ruName;
    public string ruDescription;
}

public static class KeywordDatabase
{
    private static bool isLoaded;
    private static readonly Dictionary<int, KeywordData> keywordById = new();
    private static readonly Dictionary<string, int> keywordIdByName = new(StringComparer.OrdinalIgnoreCase);

    public static string GetKeywordName(int keywordId)
    {
        EnsureLoaded();
        if (!keywordById.TryGetValue(keywordId, out KeywordData keywordData) || keywordData == null)
        {
            return string.Empty;
        }

        return LocalizationManager.ResolveLocalizedText(
            keywordData.name,
            keywordData.enName,
            keywordData.jaName,
            keywordData.zhHantName,
            keywordData.zhHansName,
            keywordData.ruName,
            keywordData.name);
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
        if (!keywordById.TryGetValue(keywordId, out KeywordData keywordData) || keywordData == null)
        {
            return string.Empty;
        }

        return LocalizationManager.ResolveLocalizedText(
            keywordData.description,
            keywordData.enDescription,
            keywordData.jaDescription,
            keywordData.zhHantDescription,
            keywordData.zhHansDescription,
            keywordData.ruDescription,
            keywordData.description);
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
            RegisterKeywordName(keywordData.name, keywordData.keywordId);
            RegisterKeywordName(keywordData.enName, keywordData.keywordId);
            RegisterKeywordName(keywordData.jaName, keywordData.keywordId);
            RegisterKeywordName(keywordData.zhHantName, keywordData.keywordId);
            RegisterKeywordName(keywordData.zhHansName, keywordData.keywordId);
            RegisterKeywordName(keywordData.ruName, keywordData.keywordId);
        }
    }

    private static void RegisterKeywordName(string keywordName, int keywordId)
    {
        if (string.IsNullOrWhiteSpace(keywordName))
        {
            return;
        }

        keywordIdByName[keywordName.Trim()] = keywordId;
    }
}
