using UnityEngine;

public static class GameRulesJsonParser
{
    private const string RulesResourcePath = "JsonData/rules";

    public static GameRulesData LoadOrDefault()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(RulesResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning("[GameRulesJsonParser] rules.json을 찾지 못해 기본 수치를 사용합니다");
            return new GameRulesData();
        }

        try
        {
            GameRulesData rules = JsonUtility.FromJson<GameRulesData>(jsonFile.text);
            if (rules != null)
            {
                return rules;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[GameRulesJsonParser] rules.json 파싱에 실패해 기본 수치를 사용합니다: {exception.Message}");
            return new GameRulesData();
        }

        Debug.LogError("[GameRulesJsonParser] rules.json 형식이 올바르지 않아 기본 수치를 사용합니다");
        return new GameRulesData();
    }
}
