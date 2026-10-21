using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// monsters.json · encounters.json 로더 (처음 접근 시 Resources에서 읽음, 테스트는 LoadFromJson 사용)
public static class MonsterDatabase
{
    private const string MonstersResourcePath = "JsonData/monsters";
    private const string EncountersResourcePath = "JsonData/encounters";

    private static Dictionary<int, MonsterDefinition> monstersById;
    private static List<EncounterDefinition> encounters;

    public static IReadOnlyCollection<MonsterDefinition> AllMonsters
    {
        get
        {
            EnsureLoaded();
            return monstersById.Values;
        }
    }

    public static IReadOnlyList<EncounterDefinition> AllEncounters
    {
        get
        {
            EnsureLoaded();
            return encounters;
        }
    }

    public static bool TryGet(int monsterId, out MonsterDefinition definition)
    {
        EnsureLoaded();
        return monstersById.TryGetValue(monsterId, out definition);
    }

    public static MonsterDefinition Get(int monsterId)
    {
        return TryGet(monsterId, out MonsterDefinition definition) ? definition : null;
    }

    public static EncounterDefinition GetEncounter(string encounterId)
    {
        EnsureLoaded();
        return encounters.Find(encounter => encounter != null && encounter.id == encounterId);
    }

    // 종류(normal · named · boss)와 계층에 맞는 조우 목록
    public static List<EncounterDefinition> GetEncounters(string kind, int tier)
    {
        EnsureLoaded();
        return encounters.FindAll(encounter => encounter != null
            && string.Equals(encounter.kind, kind, StringComparison.OrdinalIgnoreCase)
            && (tier <= 0 || encounter.tier == tier));
    }

    public static void LoadFromJson(string monstersJson, string encountersJson)
    {
        // BOM이 남아 있는 텍스트도 읽도록 제거
        monstersJson = monstersJson?.TrimStart('\uFEFF');
        encountersJson = encountersJson?.TrimStart('\uFEFF');
        MonsterDefinitionRoot monsterRoot = string.IsNullOrWhiteSpace(monstersJson)
            ? new MonsterDefinitionRoot()
            : JsonConvert.DeserializeObject<MonsterDefinitionRoot>(monstersJson) ?? new MonsterDefinitionRoot();
        EncounterDefinitionRoot encounterRoot = string.IsNullOrWhiteSpace(encountersJson)
            ? new EncounterDefinitionRoot()
            : JsonConvert.DeserializeObject<EncounterDefinitionRoot>(encountersJson) ?? new EncounterDefinitionRoot();

        monstersById = new Dictionary<int, MonsterDefinition>();
        foreach (MonsterDefinition definition in monsterRoot.monsters ?? new List<MonsterDefinition>())
        {
            if (definition == null)
            {
                continue;
            }

            if (monstersById.ContainsKey(definition.id))
            {
                Debug.LogWarning($"[MonsterDatabase] 몬스터 ID가 중복됩니다: {definition.id}");
            }

            monstersById[definition.id] = definition;
        }

        encounters = encounterRoot.encounters ?? new List<EncounterDefinition>();
    }

    public static void Reload()
    {
        monstersById = null;
        encounters = null;
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (monstersById != null && encounters != null)
        {
            return;
        }

        TextAsset monstersAsset = Resources.Load<TextAsset>(MonstersResourcePath);
        TextAsset encountersAsset = Resources.Load<TextAsset>(EncountersResourcePath);
        if (monstersAsset == null)
        {
            Debug.LogError($"[MonsterDatabase] {MonstersResourcePath}.json을 찾을 수 없습니다");
        }

        if (encountersAsset == null)
        {
            Debug.LogError($"[MonsterDatabase] {EncountersResourcePath}.json을 찾을 수 없습니다");
        }

        LoadFromJson(monstersAsset != null ? monstersAsset.text : null, encountersAsset != null ? encountersAsset.text : null);
    }
}
