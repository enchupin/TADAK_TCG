using System;
using System.Collections.Generic;
using UnityEngine;

public static class KeywordJsonParser
{
    private const string KeywordResourcePath = "JsonData/keywords";

    public static bool TryLoad(out List<KeywordData> keywords)
    {
        keywords = new List<KeywordData>();

        TextAsset jsonFile = Resources.Load<TextAsset>(KeywordResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning("[KeywordJsonParser] JsonData/keywords.json을 불러오지 못했습니다");
            return false;
        }

        KeywordJsonRoot keywordList;
        try
        {
            keywordList = JsonUtility.FromJson<KeywordJsonRoot>(jsonFile.text);
        }
        catch
        {
            Debug.LogWarning("[KeywordJsonParser] keywords.json 파싱에 실패했습니다");
            return false;
        }

        if (keywordList?.keywords == null)
        {
            Debug.LogWarning("[KeywordJsonParser] keywords.json 형식이 올바르지 않습니다");
            return false;
        }

        keywords = keywordList.keywords;
        return true;
    }

    [Serializable]
    private sealed class KeywordJsonRoot
    {
        public List<KeywordData> keywords = new List<KeywordData>();
    }
}
