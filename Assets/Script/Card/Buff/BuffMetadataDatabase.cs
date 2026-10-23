using System;
using System.Collections.Generic;

public static class BuffMetadataDatabase
{
    private static readonly Dictionary<int, BuffData> BuffDatabase = new Dictionary<int, BuffData>();
    private static readonly Dictionary<string, int> BuffIdByName = new Dictionary<string, int>(StringComparer.Ordinal);
    // 현재 언어 이름 → ID (언어 변경 시 다시 만듦)
    private static readonly Dictionary<string, int> BuffIdByDisplayName = new Dictionary<string, int>(StringComparer.Ordinal);
    private static bool displayNameIndexDirty = true;
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

    // 원문 이름 또는 현재 언어 이름으로 조회 (의도 문장의 (이름) 표기용)
    public static bool TryGetBuffIdByAnyName(string buffName, out int buffId)
    {
        if (TryGetBuffId(buffName, out buffId))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(buffName))
        {
            return false;
        }

        if (displayNameIndexDirty)
        {
            displayNameIndexDirty = false;
            BuffIdByDisplayName.Clear();
            foreach (BuffData data in BuffDatabase.Values)
            {
                if (!string.IsNullOrWhiteSpace(data?.name) && !BuffIdByDisplayName.ContainsKey(data.name))
                {
                    BuffIdByDisplayName.Add(data.name, data.buffId);
                }
            }
        }

        return BuffIdByDisplayName.TryGetValue(buffName.Trim(), out buffId);
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
            polarity = ParsePolarity(sourceData),
            stackable = sourceData.stackable,
            signed = sourceData.signed,
            name = sourceData.name,
            description = sourceData.description,
            sourceName = sourceData.name,
            sourceDescription = sourceData.description
        };
    }

    private static BuffPolarity ParsePolarity(BuffMetadataJsonData sourceData)
    {
        switch (sourceData.polarity?.Trim().ToLowerInvariant())
        {
            case "beneficial":
                return BuffPolarity.Beneficial;
            case "harmful":
                return BuffPolarity.Harmful;
            case "passive":
                return BuffPolarity.Passive;
            default:
                UnityEngine.Debug.LogWarning($"[BuffMetadataDatabase] polarity 값이 올바르지 않아 해로운 효과로 처리합니다: {sourceData.buffId} ({sourceData.polarity})");
                return BuffPolarity.Harmful;
        }
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

        // 설명은 [buff:id] 참조 · {stack} 자리표시가 남은 틀 (툴팁에서 해석)
        runtimeData.name = GameText.BuffName(runtimeData.buffId, runtimeData.sourceName);
        runtimeData.description = GameText.BuffDescription(runtimeData.buffId, runtimeData.sourceDescription);
        displayNameIndexDirty = true;
    }

    private static string GetLookupName(BuffData runtimeData)
    {
        if (!string.IsNullOrWhiteSpace(runtimeData?.sourceName))
        {
            return runtimeData.sourceName;
        }

        return runtimeData?.name ?? string.Empty;
    }
}
