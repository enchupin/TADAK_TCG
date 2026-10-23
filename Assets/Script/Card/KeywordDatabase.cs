using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KeywordData
{
    public int keywordId;
    // 한국어 원문 (JsonData/keywords.json), 번역은 Localization/<언어>/keywords.json
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
        if (!keywordById.TryGetValue(keywordId, out KeywordData keywordData) || keywordData == null)
        {
            return string.Empty;
        }

        return GameText.KeywordName(keywordId, keywordData.name);
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

        // 설명 안의 [buff:id] 참조는 현재 언어 이름으로
        return GameText.FormatPlain(GameText.KeywordDescription(keywordId, keywordData.description));
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
            // 카드 효과 데이터의 키워드 이름은 한국어 원문 기준
            RegisterKeywordName(keywordData.name, keywordData.keywordId);
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
