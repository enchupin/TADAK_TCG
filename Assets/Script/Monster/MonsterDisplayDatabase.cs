using UnityEngine;

/// <summary>
/// 몬스터 이미지 여백 측정값 · 배치 설정 로드 (Resources/JsonData)
/// 비교 씬에서 값을 바꾼 뒤 Reload로 다시 읽음
/// </summary>
public static class MonsterDisplayDatabase
{
    private static MonsterSpriteBoundsData bounds;
    private static MonsterDisplayData display;

    public static MonsterSpriteBoundsData Bounds => bounds ??= Load<MonsterSpriteBoundsData>(MonsterSpriteBoundsData.ResourcePath);
    public static MonsterDisplayData Display => display ??= Load<MonsterDisplayData>(MonsterDisplayData.ResourcePath);

    // 실행 중 값을 바꾼 경우 (비교 씬 조정) 바로 반영
    public static void SetDisplay(MonsterDisplayData data)
    {
        display = data;
    }

    public static void Reload()
    {
        bounds = null;
        display = null;
    }

    private static T Load<T>(string resourcePath) where T : new()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[MonsterDisplayDatabase] {resourcePath}.json을 찾지 못해 기본값을 사용합니다");
            return new T();
        }

        try
        {
            T data = JsonUtility.FromJson<T>(jsonFile.text);
            if (data != null)
            {
                return data;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[MonsterDisplayDatabase] {resourcePath}.json 파싱에 실패해 기본값을 사용합니다: {exception.Message}");
        }

        return new T();
    }
}
