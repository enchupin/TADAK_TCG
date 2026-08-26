using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    private const int MaxMonsterCount = 5;

    public enum SpawnMonsterType
    {
        MutantFlower,
        MutantCarnivorousPlant,
        MutantMushroom,
        MutantSweetPotato,
        MutantCarrot,
        Mirror,
        Cactus,
        WoodenPuppet,
        VoidBug,
        JackORipper,
        GiantFlowerSpider,
        Prophet,
        IceAndFireBoss,
        VoidLordBoss,
        StoneShieldGolem,
        StoneThrowGolem,
        StoneStealGolem,
        HauntedCloth,
        FireSpirit,
        RotwoodWarden,
        Priestess,
        MushroomHost,
        VoidBeast,
        FlashyScythe
    }

    [Header("몬스터 생성 프리팹")]
    [SerializeField] private MonsterPrefabFactory monsterPrefabFactory = new MonsterPrefabFactory();

    private static readonly SpawnMonsterType[][] earlyNormalNodeEncounterTable =
    {
        new[] { SpawnMonsterType.MutantFlower, SpawnMonsterType.MutantCarnivorousPlant },
        new[] { SpawnMonsterType.MutantFlower, SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.MutantCarnivorousPlant, SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneThrowGolem },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.VoidBug, SpawnMonsterType.VoidBug },
        new[] { SpawnMonsterType.MutantSweetPotato, SpawnMonsterType.MutantCarrot },
        new[] { SpawnMonsterType.FireSpirit, SpawnMonsterType.FireSpirit },
        new[] { SpawnMonsterType.Mirror },
        new[] { SpawnMonsterType.WoodenPuppet, SpawnMonsterType.WoodenPuppet }
    };

    private static readonly SpawnMonsterType[][] lateNormalNodeEncounterTable =
    {
        new[] { SpawnMonsterType.MutantFlower, SpawnMonsterType.MutantCarnivorousPlant, SpawnMonsterType.MutantMushroom },
        new[] { SpawnMonsterType.StoneShieldGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneThrowGolem, SpawnMonsterType.StoneStealGolem },
        new[] { SpawnMonsterType.VoidBug, SpawnMonsterType.VoidBug, SpawnMonsterType.VoidBug },
        new[] { SpawnMonsterType.MutantSweetPotato, SpawnMonsterType.MutantSweetPotato, SpawnMonsterType.MutantCarrot },
        new[] { SpawnMonsterType.MutantSweetPotato, SpawnMonsterType.MutantCarrot, SpawnMonsterType.MutantCarrot },
        new[] { SpawnMonsterType.JackORipper },
        new[] { SpawnMonsterType.HauntedCloth },
        new[] { SpawnMonsterType.FireSpirit, SpawnMonsterType.FireSpirit, SpawnMonsterType.FireSpirit },
        new[] { SpawnMonsterType.Cactus, SpawnMonsterType.Cactus }
    };

    private static readonly SpawnMonsterType[][] namedNodeEncounterTable =
    {
        new[] { SpawnMonsterType.RotwoodWarden },
        new[] { SpawnMonsterType.Priestess },
        new[] { SpawnMonsterType.MushroomHost },
        new[] { SpawnMonsterType.VoidBeast }
    };

    private static readonly SpawnMonsterType[][] bossNodeEncounterTable =
    {
        new[] { SpawnMonsterType.GiantFlowerSpider },
        new[] { SpawnMonsterType.Mirror, SpawnMonsterType.Mirror, SpawnMonsterType.Prophet },
        new[] { SpawnMonsterType.IceAndFireBoss },
        new[] { SpawnMonsterType.VoidBug, SpawnMonsterType.VoidBeast, SpawnMonsterType.VoidLordBoss }
    };

    [Header("스폰 위치")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private int reservedSummonCount;

    /// <summary>
    /// 노드 타입에 맞는 인카운터를 계산해 몬스터를 스폰합니다
    /// </summary>
    public List<Monster> SpawnEncounter(TrainingNodeType nodeType)
    {
        return SpawnEncounter(CreateEncounterPlan(nodeType, ResolveCurrentFloorNumber()));
    }

    /// <summary>
    /// 전달받은 몬스터 타입 목록을 실제 전투 몬스터 인스턴스로 생성합니다
    /// </summary>
    public List<Monster> SpawnEncounter(IReadOnlyList<SpawnMonsterType> encounter)
    {

        if (encounter == null || encounter.Count == 0)
        {
            return new List<Monster>();
        }

        List<Monster> spawnedMonsters = new List<Monster>(encounter.Count);

        for (int i = 0; i < encounter.Count; i++)
        {
            Monster spawnedMonster = SpawnMonsterToAvailableSlot(encounter[i]);
            if (spawnedMonster != null)
            {
                spawnedMonsters.Add(spawnedMonster);
            }
        }

        return spawnedMonsters;
    }

    /// <summary>
    /// 랭킹 모드 테스트용 허수아비 몬스터를 생성합니다
    /// </summary>
    public List<Monster> SpawnRankingScareCrow()
    {
        List<Monster> spawnedMonsters = new List<Monster>(1);
        Monster scareCrow = SpawnMonsterToAvailableSlot(typeof(ScareCrowMonster));

        if (scareCrow != null)
        {
            spawnedMonsters.Add(scareCrow);
        }

        return spawnedMonsters;
    }

    /// <summary>
    /// 노드 타입과 층수에 맞는 몬스터 조합 계획을 생성합니다
    /// </summary>
    public static List<SpawnMonsterType> CreateEncounterPlan(TrainingNodeType nodeType, int floorNumber)
    {
        SpawnMonsterType[] encounter = ResolveEncounter(nodeType, floorNumber);
        return new List<SpawnMonsterType>(encounter);
    }

    /// <summary>
    /// 노드 타입과 층수에서 등장 가능한 몬스터 조합 후보를 반환합니다
    /// </summary>
    public static List<List<SpawnMonsterType>> GetEncounterCandidates(TrainingNodeType nodeType, int floorNumber)
    {
        SpawnMonsterType[][] encounterTable = ResolveEncounterTable(nodeType, floorNumber);
        List<List<SpawnMonsterType>> candidates = new List<List<SpawnMonsterType>>(encounterTable.Length);
        for (int i = 0; i < encounterTable.Length; i++)
        {
            candidates.Add(new List<SpawnMonsterType>(encounterTable[i]));
        }

        return candidates;
    }

    /// <summary>
    /// 몬스터 조합을 순서와 무관하게 비교할 수 있는 문자열 키로 변환합니다
    /// </summary>
    public static string BuildEncounterSignature(IReadOnlyList<SpawnMonsterType> encounter)
    {
        if (encounter == null || encounter.Count == 0)
        {
            return string.Empty;
        }

        List<int> sortedMonsterTypes = new List<int>(encounter.Count);
        for (int i = 0; i < encounter.Count; i++)
        {
            sortedMonsterTypes.Add((int)encounter[i]);
        }

        sortedMonsterTypes.Sort();

        StringBuilder signatureBuilder = new StringBuilder(sortedMonsterTypes.Count * 4);
        for (int i = 0; i < sortedMonsterTypes.Count; i++)
        {
            if (i > 0)
            {
                signatureBuilder.Append(',');
            }

            signatureBuilder.Append(sortedMonsterTypes[i]);
        }

        return signatureBuilder.ToString();
    }

    /// <summary>
    /// 선택된 몬스터 조합이 후보 목록에서 몇 번째 조합인지 반환합니다
    /// </summary>
    public static int GetEncounterDisplayIndex(TrainingNodeType nodeType, int floorNumber, IReadOnlyList<SpawnMonsterType> encounter)
    {
        if (encounter == null || encounter.Count == 0)
        {
            return 0;
        }

        SpawnMonsterType[][] encounterTable = ResolveEncounterTable(nodeType, floorNumber);
        if (encounterTable == null || encounterTable.Length == 0)
        {
            return 0;
        }

        string targetSignature = BuildEncounterSignature(encounter);
        for (int i = 0; i < encounterTable.Length; i++)
        {
            if (BuildEncounterSignature(encounterTable[i]) == targetSignature)
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// 전투 중 소환되는 몬스터를 빈 스폰 위치에 생성합니다
    /// </summary>
    public Monster SpawnSummonedMonster(SpawnMonsterType monsterType)
    {
        return SpawnMonsterToAvailableSlot(monsterType);
    }

    /// <summary>
    /// 이번 행동에서 소환 가능한 자리를 미리 예약합니다
    /// </summary>
    public bool TryReserveSummonSlot()
    {
        if (GetRemainingSummonCapacity() <= 0)
        {
            return false;
        }

        reservedSummonCount++;
        return true;
    }

    /// <summary>
    /// 계획 단계에서 예약된 소환 슬롯 수를 초기화합니다
    /// </summary>
    public void ResetSummonReservations()
    {
        reservedSummonCount = 0;
    }

    /// <summary>
    /// 노드 타입과 층수에 맞는 후보 테이블에서 실제 등장 조합을 선택합니다
    /// </summary>
    private static SpawnMonsterType[] ResolveEncounter(TrainingNodeType nodeType, int floorNumber)
    {
        return PickRandomEncounter(ResolveEncounterTable(nodeType, floorNumber));
    }

    /// <summary>
    /// 노드 타입과 층수에 대응하는 인카운터 후보 테이블을 반환합니다
    /// </summary>
    private static SpawnMonsterType[][] ResolveEncounterTable(TrainingNodeType nodeType, int floorNumber)
    {
        switch (nodeType)
        {
            case TrainingNodeType.Named:
                return namedNodeEncounterTable;
            case TrainingNodeType.Boss:
                return bossNodeEncounterTable;
            case TrainingNodeType.Monster:
                return IsLateGameEncounterFloor(floorNumber)
                    ? lateNormalNodeEncounterTable
                    : earlyNormalNodeEncounterTable;
            default:
                return Array.Empty<SpawnMonsterType[]>();
        }
    }

    /// <summary>
    /// 후보 테이블에서 무작위 몬스터 조합 하나를 선택합니다
    /// </summary>
    private static SpawnMonsterType[] PickRandomEncounter(SpawnMonsterType[][] encounterTable)
    {
        if (encounterTable == null || encounterTable.Length == 0)
        {
            return new SpawnMonsterType[0];
        }

        return encounterTable[UnityEngine.Random.Range(0, encounterTable.Length)];
    }

    /// <summary>
    /// 현재 층수가 후반부 일반 전투 테이블을 사용할 구간인지 확인합니다
    /// </summary>
    private static bool IsLateGameEncounterFloor(int floorNumber)
    {
        return floorNumber >= 9;
    }

    /// <summary>
    /// 현재 진행 상태에서 전투가 발생한 층 번호를 계산합니다
    /// </summary>
    private static int ResolveCurrentFloorNumber()
    {
        if (TrainingRunState.PendingNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode))
        {
            return pendingNode.stageIndex;
        }

        if (TrainingRunState.CurrentNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.CurrentNodeId.Value, out TrainingMapNodeData currentNode))
        {
            return currentNode.stageIndex;
        }

        return 1;
    }

    /// <summary>
    /// 몬스터 타입을 공통 프리팹 기반으로 생성해 빈 스폰 위치에 배치합니다
    /// </summary>
    private Monster SpawnMonsterToAvailableSlot(SpawnMonsterType monsterType)
    {
        if (!TryGetAvailableSpawnPoint(out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterType}' 생성이 취소되었습니다");
            return null;
        }

        Monster spawnedMonster = monsterPrefabFactory.CreateMonster(monsterType, spawnPoint);
        if (spawnedMonster == null)
        {
            return null;
        }

        return spawnedMonster;
    }

    /// <summary>
    /// 런타임에 직접 지정한 몬스터 컴포넌트 타입을 빈 스폰 위치에 생성합니다
    /// </summary>
    private Monster SpawnMonsterToAvailableSlot(Type monsterComponentType)
    {
        if (!TryGetAvailableSpawnPoint(out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterComponentType?.Name}' 생성이 취소되었습니다");
            return null;
        }

        Monster spawnedMonster = monsterPrefabFactory.CreateMonster(monsterComponentType, spawnPoint);
        if (spawnedMonster == null)
        {
            return null;
        }

        return spawnedMonster;
    }

    /// <summary>
    /// 현재 사용할 수 있는 비어 있는 스폰 위치를 찾습니다
    /// </summary>
    private bool TryGetAvailableSpawnPoint(out Transform availableSpawnPoint)
    {
        availableSpawnPoint = null;

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치가 설정되지 않았습니다");
            return false;
        }

        int spawnPointCount = Mathf.Min(spawnPoints.Count, MaxMonsterCount);
        for (int i = 0; i < spawnPointCount; i++)
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

    /// <summary>
    /// 현재 전투에서 추가 소환 가능한 남은 슬롯 수를 계산합니다
    /// </summary>
    private int GetRemainingSummonCapacity()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return 0;
        }

        int occupiedCount = 0;
        int spawnPointCount = Mathf.Min(spawnPoints.Count, MaxMonsterCount);
        for (int i = 0; i < spawnPointCount; i++)
        {
            if (IsSpawnPointOccupied(spawnPoints[i]))
            {
                occupiedCount++;
            }
        }

        return Mathf.Max(0, spawnPointCount - occupiedCount - reservedSummonCount);
    }

    /// <summary>
    /// 지정한 스폰 위치에 살아 있는 몬스터가 이미 있는지 확인합니다
    /// </summary>
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
            if (monster != null && monster.gameObject.activeSelf && !monster.IsDead())
            {
                return true;
            }
        }

        return false;
    }

}
