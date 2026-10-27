using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    // 동시 적 수 상한은 rules.json의 combat.maxEnemies에서 읽음
    private static int MaxMonsterCount => Mathf.Max(0, GameRules.Combat.maxEnemies);

    // 몬스터 생성 프리팹
    private readonly MonsterPrefabFactory monsterPrefabFactory = new MonsterPrefabFactory();

    [Header("스폰 위치")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private int reservedSummonCount;
    // 이번 조우에서 몬스터 ID별 생성 수 (R-86 패턴 엇갈림 순번)
    private readonly System.Collections.Generic.Dictionary<int, int> spawnedCountById = new System.Collections.Generic.Dictionary<int, int>();
    // 씬에 놓인 스폰 위치 (조우 좌표가 없는 칸 · 정리 시 복귀용)
    private readonly List<SpawnSlot> sceneSlotPositions = new List<SpawnSlot>();
    // 이번 조우의 시작 몬스터 수, 소환은 그다음 칸부터 사용
    private int startMonsterCount;

    private void Awake()
    {
        CacheSceneSlotPositions();
        ClearEncounter();
    }

    public void ClearEncounter()
    {
        reservedSummonCount = 0;
        spawnedCountById.Clear();
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                point.gameObject.SetActive(false);
            }
        }

        // 조우 좌표로 옮겼던 스폰 위치를 씬 위치로 되돌림
        startMonsterCount = 0;
        ApplySlotPositions(sceneSlotPositions);
    }

    /// <summary>
    /// 노드 타입에 맞는 인카운터를 계산해 몬스터를 스폰합니다
    /// </summary>
    public List<Monster> SpawnEncounter(TrainingNodeType nodeType)
    {
        return SpawnEncounter(CreateEncounterPlan(nodeType, ResolveCurrentFloorNumber()));
    }

    // 조우 ID(E101 · NM02-1 · BS01)로 생성
    public List<Monster> SpawnEncounter(string encounterId)
    {
        EncounterDefinition encounter = MonsterDatabase.GetEncounter(encounterId);
        if (encounter == null)
        {
            Debug.LogWarning($"[MonsterSpawner] 조우 {encounterId}를 찾을 수 없습니다");
            return new List<Monster>();
        }

        return SpawnEncounter(encounter.monsters);
    }

    /// <summary>
    /// 전달받은 몬스터 타입 목록을 실제 전투 몬스터 인스턴스로 생성합니다
    /// </summary>
    public List<Monster> SpawnEncounter(IReadOnlyList<int> encounter)
    {
        // 몬스터 타입 대신 v1.0 몬스터 ID(4자리) 목록

        if (encounter == null || encounter.Count == 0)
        {
            return new List<Monster>();
        }

        List<Monster> spawnedMonsters = new List<Monster>(encounter.Count);
        // 저장 데이터에는 조우 ID가 없어 몬스터 목록으로 조우를 찾아 칸 좌표 적용
        ApplyEncounterLayout(SpawnLayoutResolver.FindEncounterId(encounter, MonsterDatabase.AllEncounters), encounter.Count);

        for (int i = 0; i < encounter.Count; i++)
        {
            // i번째 몬스터는 조우 좌표의 i번 칸
            Monster spawnedMonster = SpawnMonsterToAvailableSlot(encounter[i], i);
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
        ApplyEncounterLayout(SpawnLayoutData.RankingScareCrowId, 1);
        Monster scareCrow = SpawnMonsterToAvailableSlot(typeof(ScareCrowMonster), 0);

        if (scareCrow != null)
        {
            spawnedMonsters.Add(scareCrow);
        }

        return spawnedMonsters;
    }

    /// <summary>
    /// 노드 타입과 층수에 맞는 몬스터 조합 계획을 생성합니다
    /// </summary>
    public static List<int> CreateEncounterPlan(TrainingNodeType nodeType, int floorNumber)
    {
        EncounterDefinition encounter = ResolveEncounter(nodeType, floorNumber);
        return encounter != null ? new List<int>(encounter.monsters) : new List<int>();
    }

    /// <summary>
    /// 노드 타입과 층수에서 등장 가능한 몬스터 조합 후보를 반환합니다
    /// </summary>
    public static List<List<int>> GetEncounterCandidates(TrainingNodeType nodeType, int floorNumber)
    {
        List<EncounterDefinition> encounterTable = ResolveEncounterTable(nodeType, floorNumber);
        List<List<int>> candidates = new List<List<int>>(encounterTable.Count);
        for (int i = 0; i < encounterTable.Count; i++)
        {
            candidates.Add(new List<int>(encounterTable[i].monsters));
        }

        return candidates;
    }

    /// <summary>
    /// 몬스터 조합을 순서와 무관하게 비교할 수 있는 문자열 키로 변환합니다
    /// </summary>
    public static string BuildEncounterSignature(IReadOnlyList<int> encounter)
    {
        if (encounter == null || encounter.Count == 0)
        {
            return string.Empty;
        }

        List<int> sortedMonsterTypes = new List<int>(encounter.Count);
        for (int i = 0; i < encounter.Count; i++)
        {
            sortedMonsterTypes.Add(encounter[i]);
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
    public static int GetEncounterDisplayIndex(TrainingNodeType nodeType, int floorNumber, IReadOnlyList<int> encounter)
    {
        if (encounter == null || encounter.Count == 0)
        {
            return 0;
        }

        List<EncounterDefinition> encounterTable = ResolveEncounterTable(nodeType, floorNumber);
        if (encounterTable == null || encounterTable.Count == 0)
        {
            return 0;
        }

        string targetSignature = BuildEncounterSignature(encounter);
        for (int i = 0; i < encounterTable.Count; i++)
        {
            if (BuildEncounterSignature(encounterTable[i].monsters) == targetSignature)
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// 전투 중 소환되는 몬스터를 빈 스폰 위치에 생성합니다
    /// </summary>
    public Monster SpawnSummonedMonster(int monsterId)
    {
        return SpawnMonsterToAvailableSlot(monsterId);
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
    private static EncounterDefinition ResolveEncounter(TrainingNodeType nodeType, int floorNumber)
    {
        return PickRandomEncounter(ResolveEncounterTable(nodeType, floorNumber));
    }

    /// <summary>
    /// 노드 타입과 층수에 대응하는 인카운터 후보 테이블을 반환합니다
    /// </summary>
    private static List<EncounterDefinition> ResolveEncounterTable(TrainingNodeType nodeType, int floorNumber)
    {
        // 8절: 층이 속한 계층의 조우 풀 (보스는 BS01 ~ BS04)
        switch (nodeType)
        {
            case TrainingNodeType.Named:
                return MonsterDatabase.GetEncounters(MonsterDefinition.KindNamed, ResolveEncounterTier(floorNumber));
            case TrainingNodeType.Boss:
                return MonsterDatabase.GetEncounters(MonsterDefinition.KindBoss, 0);
            case TrainingNodeType.Monster:
                return MonsterDatabase.GetEncounters(MonsterDefinition.KindNormal, ResolveEncounterTier(floorNumber));
            default:
                return new List<EncounterDefinition>();
        }
    }

    /// <summary>
    /// 후보 테이블에서 무작위 몬스터 조합 하나를 선택합니다
    /// </summary>
    private static EncounterDefinition PickRandomEncounter(List<EncounterDefinition> encounterTable)
    {
        if (encounterTable == null || encounterTable.Count == 0)
        {
            return null;
        }

        return encounterTable[UnityEngine.Random.Range(0, encounterTable.Count)];
    }

    /// <summary>
    /// 현재 층수가 후반부 일반 전투 테이블을 사용할 구간인지 확인합니다
    /// </summary>
    // 층 번호를 rules.json training.map.segments 기준 계층(1 ~ 4)으로 변환, 구간 밖 층은 직전 구간 계층
    public static int ResolveEncounterTier(int floorNumber)
    {
        int tier = 1;
        foreach (MapSegmentData segment in GameRules.Training.map.segments)
        {
            if (segment == null)
            {
                continue;
            }

            if (floorNumber >= segment.fromFloor && floorNumber <= segment.toFloor)
            {
                return segment.tier;
            }

            if (floorNumber > segment.toFloor)
            {
                tier = Mathf.Max(tier, segment.tier);
            }
        }

        return tier;
    }

    /// <summary>
    /// 현재 진행 상태에서 전투가 발생한 층 번호를 계산합니다
    /// </summary>
    private static int ResolveCurrentFloorNumber()
    {
        if (TrainingRunState.PendingNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.PendingNodeId.Value, out TrainingMapNodeData pendingNode))
        {
            return pendingNode.FloorNumber;
        }

        if (TrainingRunState.CurrentNodeId.HasValue
            && TrainingRunState.TryGetNode(TrainingRunState.CurrentNodeId.Value, out TrainingMapNodeData currentNode))
        {
            return currentNode.FloorNumber;
        }

        return 1;
    }

    /// <summary>
    /// 몬스터 타입을 공통 프리팹 기반으로 생성해 빈 스폰 위치에 배치합니다
    /// </summary>
    // preferredSlot: 비어 있으면 먼저 쓸 칸 (시작 몬스터), 음수면 소환 칸 순서
    private Monster SpawnMonsterToAvailableSlot(int monsterId, int preferredSlot = -1)
    {
        if (!TryGetAvailableSpawnPoint(preferredSlot, out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterId}' 생성이 취소되었습니다");
            return null;
        }

        spawnedCountById.TryGetValue(monsterId, out int staggerIndex);
        Monster monster = monsterPrefabFactory.CreateMonster(monsterId, spawnPoint, staggerIndex);
        if (monster != null)
        {
            spawnedCountById[monsterId] = staggerIndex + 1;
        }

        return monster;
    }

    /// <summary>
    /// 런타임에 직접 지정한 몬스터 컴포넌트 타입을 빈 스폰 위치에 생성합니다
    /// </summary>
    public Monster SpawnMonsterToAvailableSlot(Type monsterComponentType, int preferredSlot = -1)
    {
        if (!TryGetAvailableSpawnPoint(preferredSlot, out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterComponentType?.Name}' 생성이 취소되었습니다");
            return null;
        }

        return monsterPrefabFactory.CreateMonster(monsterComponentType, spawnPoint);
    }

    /// <summary>
    /// 현재 사용할 수 있는 비어 있는 스폰 위치를 찾습니다
    /// </summary>
    private bool TryGetAvailableSpawnPoint(int preferredSlot, out Transform availableSpawnPoint)
    {
        availableSpawnPoint = null;

        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] 스폰 위치가 설정되지 않았습니다");
            return false;
        }

        int spawnPointCount = Mathf.Min(spawnPoints.Count, MaxMonsterCount);
        if (preferredSlot >= 0 && preferredSlot < spawnPointCount
            && spawnPoints[preferredSlot] != null && !IsSpawnPointOccupied(spawnPoints[preferredSlot]))
        {
            availableSpawnPoint = spawnPoints[preferredSlot];
            return true;
        }

        // 시작 몬스터 다음 칸(소환 자리)부터, 다 차면 앞 번호의 빈 칸
        foreach (int i in SpawnLayoutResolver.SummonSlotOrder(startMonsterCount, spawnPointCount))
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
        // 사망 처리가 끝나 프리팹이 비활성화된 슬롯만 재사용
        return spawnPoint != null && spawnPoint.gameObject.activeSelf;
    }

    /// <summary>
    /// 조우 ID의 칸 좌표로 스폰 위치를 옮깁니다 (spawnLayouts.json)
    /// </summary>
    private void ApplyEncounterLayout(string layoutId, int monsterCount)
    {
        startMonsterCount = monsterCount;
        // Awake보다 먼저 호출된 경우 대비
        if (sceneSlotPositions.Count != spawnPoints.Count)
        {
            CacheSceneSlotPositions();
        }

        SpawnLayoutEntry entry = SpawnLayoutDatabase.Data.Find(layoutId);
        if (entry == null || entry.slots == null || entry.slots.Count < monsterCount)
        {
            Debug.LogWarning($"[MonsterSpawner] '{layoutId ?? "(조우 없음)"}'의 스폰 좌표가 부족해 씬 스폰 위치를 함께 사용합니다");
        }

        ApplySlotPositions(SpawnLayoutResolver.ResolveSlots(entry, sceneSlotPositions));
    }

    /// <summary>
    /// 씬에 놓인 스폰 위치를 기억합니다
    /// </summary>
    // 스폰 위치는 MonsterSpawner (n) 아래 몬스터 프리팹이라 부모 위치가 더해짐
    // 좌표는 이 컴포넌트가 붙은 MonsterSpawnPositions(캔버스 우측 중앙) 기준 피벗 위치로 읽고 씀
    private void CacheSceneSlotPositions()
    {
        sceneSlotPositions.Clear();
        foreach (Transform point in spawnPoints)
        {
            Vector3 position = point == null ? Vector3.zero : transform.InverseTransformPoint(point.position);
            sceneSlotPositions.Add(new SpawnSlot(position.x, position.y));
        }
    }

    private void ApplySlotPositions(IReadOnlyList<SpawnSlot> slots)
    {
        int count = Mathf.Min(spawnPoints.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null || slots[i] == null)
            {
                continue;
            }

            float z = transform.InverseTransformPoint(point.position).z;
            point.position = transform.TransformPoint(new Vector3(slots[i].x, slots[i].y, z));
        }
    }
}
