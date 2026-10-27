using UnityEngine;

/// <summary>
/// 조우별 스폰 좌표 로드 (Resources/JsonData/spawnLayouts.json)
/// 조정 씬에서 값을 바꾼 뒤 SetData로 바로 반영
/// </summary>
public static class SpawnLayoutDatabase
{
    private static SpawnLayoutData data;

    public static SpawnLayoutData Data => data ??= Load();

    public static void SetData(SpawnLayoutData value)
    {
        data = value;
    }

    public static void Reload()
    {
        data = null;
    }

    private static SpawnLayoutData Load()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(SpawnLayoutData.ResourcePath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"[SpawnLayoutDatabase] {SpawnLayoutData.ResourcePath}.json을 찾지 못해 씬 스폰 위치를 사용합니다");
            return new SpawnLayoutData();
        }

        try
        {
            SpawnLayoutData loaded = JsonUtility.FromJson<SpawnLayoutData>(jsonFile.text);
            if (loaded != null)
            {
                return loaded;
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[SpawnLayoutDatabase] {SpawnLayoutData.ResourcePath}.json 파싱에 실패해 씬 스폰 위치를 사용합니다: {exception.Message}");
        }

        return new SpawnLayoutData();
    }
}
