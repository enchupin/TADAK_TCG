using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    private enum SpawnMonsterType
    {
        MutantFlower,
        MutantCarnivorousPlant,
        MutantMushroom,
        JackORipper,
        StoneShieldGolem,
        StoneThrowGolem,
        StoneStealGolem,
        RotwoodWarden,
        Priestess,
        MushroomHost
    }

    private static readonly SpawnMonsterType[][] normalNodeEncounterTable =
    {
        new[] { SpawnMonsterType.MutantFlower, SpawnMonsterType.MutantCarnivorousPlant },
        new[] { SpawnMonsterType.MutantFlower, SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.MutantCarnivorousPlant, SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.JackORipper },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneThrowGolem },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem }
    };

    private static readonly SpawnMonsterType[][] namedNodeEncounterTable =
    {
        new[] { SpawnMonsterType.RotwoodWarden },
        new[] { SpawnMonsterType.Priestess },
        new[] { SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.MushroomHost }
    };

    [Header("일반 몬스터 프리팹")]
    [SerializeField] private GameObject mutantFlowerPrefab;
    [SerializeField] private GameObject mutantCarnivorousPlantPrefab;
    [SerializeField] private GameObject mutantMushroomPrefab;
    [SerializeField] private GameObject jackORipperPrefab;
    [SerializeField] private GameObject stoneShieldGolemPrefab;
    [SerializeField] private GameObject stoneThrowGolemPrefab;
    [SerializeField] private GameObject stoneStealGolemPrefab;

    [Header("네임드 몬스터 프리팹")]
    [SerializeField] private GameObject rotwoodWardenPrefab;
    [SerializeField] private GameObject priestessPrefab;
    [SerializeField] private GameObject mushroomHostPrefab;

    [Header("스폰 위치")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    public List<Monster> SpawnEncounter(TrainingNodeType nodeType)
    {
        SpawnMonsterType[] encounter = ResolveEncounter(nodeType);
        List<Monster> spawnedMonsters = new List<Monster>(encounter.Length);

        for (int i = 0; i < encounter.Length; i++)
        {
            GameObject monsterPrefab = ResolveMonsterPrefab(encounter[i]);
            if (monsterPrefab == null)
            {
                Debug.LogError($"[MonsterSpawner] 몬스터 프리팹이 설정되지 않았습니다. type={encounter[i]}");
                continue;
            }

            Monster spawnedMonster = SpawnMonster(monsterPrefab, i);
            if (spawnedMonster != null)
            {
                spawnedMonsters.Add(spawnedMonster);
            }
        }

        return spawnedMonsters;
    }

    private SpawnMonsterType[] ResolveEncounter(TrainingNodeType nodeType)
    {
        switch (nodeType)
        {
            case TrainingNodeType.Named:
                return namedNodeEncounterTable[Random.Range(0, namedNodeEncounterTable.Length)];
            case TrainingNodeType.Boss:
                return new[] { SpawnMonsterType.JackORipper };
            case TrainingNodeType.Monster:
            default:
                return normalNodeEncounterTable[Random.Range(0, normalNodeEncounterTable.Length)];
        }
    }

    private Monster SpawnMonster(GameObject monsterPrefab, int spawnIndex)
    {
        Transform spawnPoint = GetSpawnPoint(spawnIndex);
        GameObject spawnedObject;

        if (spawnPoint != null)
        {
            spawnedObject = Instantiate(monsterPrefab, spawnPoint, false);
            spawnedObject.transform.localPosition = Vector3.zero;
            spawnedObject.transform.localRotation = Quaternion.identity;
            spawnedObject.transform.localScale = Vector3.one;
        }
        else
        {
            spawnedObject = Instantiate(monsterPrefab);
        }

        if (!spawnedObject.TryGetComponent(out Monster monster))
        {
            Debug.LogError($"[MonsterSpawner] 생성된 프리팹 '{spawnedObject.name}'에 Monster 컴포넌트가 없습니다");
            return null;
        }

        return monster;
    }

    private Transform GetSpawnPoint(int spawnIndex)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
            return null;

        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Count)
            return spawnPoints[spawnPoints.Count - 1];

        return spawnPoints[spawnIndex];
    }

    private GameObject ResolveMonsterPrefab(SpawnMonsterType monsterType)
    {
        switch (monsterType)
        {
            case SpawnMonsterType.MutantFlower:
                return mutantFlowerPrefab;
            case SpawnMonsterType.MutantCarnivorousPlant:
                return mutantCarnivorousPlantPrefab;
            case SpawnMonsterType.MutantMushroom:
                return mutantMushroomPrefab;
            case SpawnMonsterType.JackORipper:
                return jackORipperPrefab;
            case SpawnMonsterType.StoneShieldGolem:
                return stoneShieldGolemPrefab;
            case SpawnMonsterType.StoneThrowGolem:
                return stoneThrowGolemPrefab;
            case SpawnMonsterType.StoneStealGolem:
                return stoneStealGolemPrefab;
            case SpawnMonsterType.RotwoodWarden:
                return rotwoodWardenPrefab;
            case SpawnMonsterType.Priestess:
                return priestessPrefab;
            case SpawnMonsterType.MushroomHost:
                return mushroomHostPrefab;
            default:
                return null;
        }
    }
}
