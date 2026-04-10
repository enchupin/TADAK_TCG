using System;
using System.Collections.Generic;
using UnityEngine;

public static class BuffMetadataDatabase
{
    private const string BuffResourcePath = "JsonData/buffs";

    private static readonly Dictionary<int, BuffData> BuffDatabase = new Dictionary<int, BuffData>();
    private static readonly Dictionary<string, int> BuffIdByName = new Dictionary<string, int>(StringComparer.Ordinal);
    private static bool isLoaded;

    public static void Preload()
    {
        EnsureLoaded();
    }

    public static bool TryGetBuffData(int buffId, out BuffData data)
    {
        EnsureLoaded();
        return BuffDatabase.TryGetValue(buffId, out data);
    }

    public static bool TryGetBuffId(string buffName, out int buffId)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(buffName))
        {
            buffId = 0;
            return false;
        }

        return BuffIdByName.TryGetValue(buffName, out buffId);
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        isLoaded = true;
        BuffDatabase.Clear();
        BuffIdByName.Clear();

        TextAsset jsonFile = Resources.Load<TextAsset>(BuffResourcePath);
        if (jsonFile == null)
        {
            Debug.LogError("[BuffMetadataDatabase] buffs.json을 불러오지 못했습니다");
            return;
        }

        BuffMetadataList buffList = JsonUtility.FromJson<BuffMetadataList>(jsonFile.text);
        if (buffList?.buffs == null)
        {
            Debug.LogError("[BuffMetadataDatabase] buffs.json 형식이 올바르지 않습니다");
            return;
        }

        foreach (BuffData sourceData in buffList.buffs)
        {
            if (sourceData == null)
            {
                continue;
            }

            BuffData runtimeData = new BuffData
            {
                buffId = sourceData.buffId,
                name = sourceData.name,
                description = sourceData.description
            };

            if (BuffDatabase.ContainsKey(runtimeData.buffId))
            {
                Debug.LogWarning($"[BuffMetadataDatabase] 중복 buffId가 감지되었습니다: {runtimeData.buffId}");
                continue;
            }

            BuffDatabase.Add(runtimeData.buffId, runtimeData);

            if (string.IsNullOrWhiteSpace(runtimeData.name))
            {
                continue;
            }

            if (BuffIdByName.ContainsKey(runtimeData.name))
            {
                Debug.LogWarning($"[BuffMetadataDatabase] 중복 버프 이름이 감지되었습니다: {runtimeData.name}");
                continue;
            }

            BuffIdByName.Add(runtimeData.name, runtimeData.buffId);
        }
    }

    [Serializable]
    private sealed class BuffMetadataList
    {
        public List<BuffData> buffs = new List<BuffData>();
    }
}
