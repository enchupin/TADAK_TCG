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

            Monster spawnedMonster = SpawnMonsterToAvailableSlot(monsterPrefab);
            if (spawnedMonster != null)
            {
                spawnedMonsters.Add(spawnedMonster);
            }
        }

        return spawnedMonsters;
    }

    public Monster SpawnSummonedMonster(GameObject monsterPrefab)
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] 소환할 몬스터 프리팹이 비어 있습니다");
            return null;
        }

        return SpawnMonsterToAvailableSlot(monsterPrefab);
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

    private Monster SpawnMonsterToAvailableSlot(GameObject monsterPrefab)
    {
        if (!TryGetAvailableSpawnPoint(out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterPrefab.name}' 소환을 생략합니다");
            return null;
        }

        return SpawnMonster(monsterPrefab, spawnPoint);
    }

    private Monster SpawnMonster(GameObject monsterPrefab, Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[MonsterSpawner] 유효한 스폰 위치가 없어 '{monsterPrefab.name}' 생성에 실패했습니다");
            return null;
        }

        GameObject spawnedObject = Instantiate(monsterPrefab, spawnPoint, false);
        spawnedObject.transform.localPosition = Vector3.zero;
        spawnedObject.transform.localRotation = Quaternion.identity;
        spawnedObject.transform.localScale = Vector3.one;

        if (!spawnedObject.TryGetComponent(out Monster monster))
        {
            Debug.LogError($"[MonsterSpawner] 생성된 프리팹 '{spawnedObject.name}'에 Monster 컴포넌트가 없습니다");
            return null;
        }

        return monster;
    }

    private bool TryGetAvailableSpawnPoint(out Transform availableSpawnPoint)
    {
        availableSpawnPoint = null;

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치가 설정되지 않았습니다");
            return false;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            if (spawnPoint == null || IsSpawnPointOccupied(spawnPoint))
            {
                continue;
            }

            availableSpawnPoint = spawnPoint;
            return true;
        }

        return false;
    }

    private bool IsSpawnPointOccupied(Transform spawnPoint)
    {
        if (spawnPoint == null)
        {
            return false;
        }

        Monster[] monsters = spawnPoint.GetComponentsInChildren<Monster>(true);
        for (int i = 0; i < monsters.Length; i++)
        {
            Monster monster = monsters[i];
            if (monster != null && !monster.IsDead())
            {
                return true;
            }
        }

        return false;
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
