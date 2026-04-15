using System;
using System.Collections.Generic;

public static class BuffMetadataDatabase
{
    private static readonly Dictionary<int, BuffData> BuffDatabase = new Dictionary<int, BuffData>();
    private static readonly Dictionary<string, int> BuffIdByName = new Dictionary<string, int>(StringComparer.Ordinal);
    private static bool isLoaded;

    public static void Preload()
    {
        EnsureLoaded();
    }

    public static void RefreshLocalizedText()
    {
        EnsureLoaded();
        ApplyCurrentLanguage();
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

        if (!BuffMetadataJsonParser.TryLoad(out List<BuffMetadataJsonData> parsedBuffs))
        {
            return;
        }

        foreach (BuffMetadataJsonData sourceData in parsedBuffs)
        {
            AddRuntimeData(sourceData);
        }
    }

    private static void AddRuntimeData(BuffMetadataJsonData sourceData)
    {
        if (sourceData == null || sourceData.buffId <= 0)
        {
            return;
        }

        BuffData runtimeData = CreateRuntimeData(sourceData);
        ApplyLocalizedText(runtimeData);

        if (BuffDatabase.ContainsKey(runtimeData.buffId))
        {
            UnityEngine.Debug.LogWarning($"[BuffMetadataDatabase] 중복 buffId가 감지되었습니다: {runtimeData.buffId}");
            return;
        }

        BuffDatabase.Add(runtimeData.buffId, runtimeData);

        string lookupName = GetLookupName(runtimeData);
        if (string.IsNullOrWhiteSpace(lookupName))
        {
            return;
        }

        if (BuffIdByName.ContainsKey(lookupName))
        {
            UnityEngine.Debug.LogWarning($"[BuffMetadataDatabase] 중복 버프 이름이 감지되었습니다: {lookupName}");
            return;
        }

        BuffIdByName.Add(lookupName, runtimeData.buffId);
    }

    private static BuffData CreateRuntimeData(BuffMetadataJsonData sourceData)
    {
        return new BuffData
        {
            buffId = sourceData.buffId,
            name = sourceData.name,
            description = sourceData.description,
            koName = sourceData.name,
            koDescription = sourceData.description,
            enName = sourceData.enName,
            enDescription = sourceData.enDescription,
            jaName = sourceData.jaName,
            jaDescription = sourceData.jaDescription,
            zhHantName = sourceData.zhHantName,
            zhHantDescription = sourceData.zhHantDescription,
            zhHansName = sourceData.zhHansName,
            zhHansDescription = sourceData.zhHansDescription
        };
    }

    private static void ApplyCurrentLanguage()
    {
        foreach (BuffData runtimeData in BuffDatabase.Values)
        {
            ApplyLocalizedText(runtimeData);
        }
    }

    private static void ApplyLocalizedText(BuffData runtimeData)
    {
        if (runtimeData == null)
        {
            return;
        }

        LocalizationLanguage currentLanguage = LocalizationManager.GetCurrentLanguage();
        runtimeData.name = LocalizationManager.ResolveLocalizedText(
            currentLanguage,
            runtimeData.koName,
            runtimeData.enName,
            runtimeData.jaName,
            runtimeData.zhHantName,
            runtimeData.zhHansName,
            runtimeData.name);
        runtimeData.description = LocalizationManager.ResolveLocalizedText(
            currentLanguage,
            runtimeData.koDescription,
            runtimeData.enDescription,
            runtimeData.jaDescription,
            runtimeData.zhHantDescription,
            runtimeData.zhHansDescription,
            runtimeData.description);
    }

    private static string GetLookupName(BuffData runtimeData)
    {
        if (!string.IsNullOrWhiteSpace(runtimeData?.koName))
        {
            return runtimeData.koName;
        }

        return runtimeData?.name ?? string.Empty;
    }
}
