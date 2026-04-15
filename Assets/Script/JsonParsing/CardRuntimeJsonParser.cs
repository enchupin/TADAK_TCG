using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardRuntimeJsonParser
{
    private const string JsonResourcePath = "JsonData";
    private const string CardGroupsResourcePath = "JsonData/cardGroups";

    public static Dictionary<int, List<int>> LoadRuntimeKeywords()
    {
        Dictionary<int, List<int>> keywordCache = new Dictionary<int, List<int>>();
        TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(JsonResourcePath);
        foreach (TextAsset jsonFile in jsonFiles)
        {
            if (jsonFile == null || string.IsNullOrWhiteSpace(jsonFile.name))
            {
                continue;
            }

            if (!jsonFile.name.EndsWith("Cards"))
            {
                continue;
            }

            CardRuntimeJsonRoot root;
            try
            {
                root = JsonUtility.FromJson<CardRuntimeJsonRoot>(jsonFile.text);
            }
            catch
            {
                continue;
            }

            if (root?.cards == null)
            {
                continue;
            }

            foreach (CardRuntimeJsonData jsonCard in root.cards)
            {
                if (jsonCard == null || jsonCard.cardId <= 0)
                {
                    continue;
                }

                keywordCache[jsonCard.cardId] = jsonCard.keywords != null
                    ? new List<int>(jsonCard.keywords)
                    : new List<int>();
            }
        }

        return keywordCache;
    }

    public static bool TryLoadCardGroups(out Dictionary<string, List<int>> groups)
    {
        groups = new Dictionary<string, List<int>>();

        TextAsset jsonFile = Resources.Load<TextAsset>(CardGroupsResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning("[CardRuntimeJsonParser] cardGroups.json을 불러오지 못했습니다");
            return false;
        }

        CardGroupJsonRoot root;
        try
        {
            root = JsonUtility.FromJson<CardGroupJsonRoot>(jsonFile.text);
        }
        catch
        {
            Debug.LogWarning("[CardRuntimeJsonParser] cardGroups.json 파싱에 실패했습니다");
            return false;
        }

        if (root?.groups == null)
        {
            Debug.LogWarning("[CardRuntimeJsonParser] cardGroups.json 형식이 올바르지 않습니다");
            return false;
        }

        foreach (CardGroupJsonData groupData in root.groups)
        {
            if (groupData == null || string.IsNullOrWhiteSpace(groupData.groupName))
            {
                continue;
            }

            groups[groupData.groupName] = groupData.cardIds != null
                ? new List<int>(groupData.cardIds)
                : new List<int>();
        }

        return true;
    }

    [Serializable]
    private sealed class CardRuntimeJsonRoot
    {
        public List<CardRuntimeJsonData> cards = new List<CardRuntimeJsonData>();
    }

    [Serializable]
    private sealed class CardRuntimeJsonData
    {
        public int cardId = 0;
        public List<int> keywords = null;
    }

    [Serializable]
    private sealed class CardGroupJsonRoot
    {
        public List<CardGroupJsonData> groups = new List<CardGroupJsonData>();
    }

    [Serializable]
    private sealed class CardGroupJsonData
    {
        public string groupName = string.Empty;
        public List<int> cardIds = null;
    }
}
