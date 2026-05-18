using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        VoidBeast
    }

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

    [Header("일반 몬스터 프리팹")]
    [SerializeField] private GameObject mutantFlowerPrefab;
    [SerializeField] private GameObject mutantCarnivorousPlantPrefab;
    [SerializeField] private GameObject mutantMushroomPrefab;
    [SerializeField] private GameObject mutantSweetPotatoPrefab;
    [SerializeField] private GameObject mutantCarrotPrefab;
    [SerializeField] private GameObject mirrorPrefab;
    [SerializeField] private GameObject cactusPrefab;
    [SerializeField] private GameObject woodenPuppetPrefab;
    [SerializeField] private GameObject voidBugPrefab;
    [SerializeField] private GameObject jackORipperPrefab;
    [SerializeField] private GameObject giantFlowerSpiderPrefab;
    [SerializeField] private GameObject prophetPrefab;
    [SerializeField] private GameObject iceAndFireBossPrefab;
    [SerializeField] private GameObject voidLordBossPrefab;
    [SerializeField] private GameObject stoneShieldGolemPrefab;
    [SerializeField] private GameObject stoneThrowGolemPrefab;
    [SerializeField] private GameObject stoneStealGolemPrefab;
    [SerializeField] private GameObject hauntedClothPrefab;
    [SerializeField] private GameObject fireSpiritPrefab;

    [Header("네임드 몬스터 프리팹")]
    [SerializeField] private GameObject rotwoodWardenPrefab;
    [SerializeField] private GameObject priestessPrefab;
    [SerializeField] private GameObject mushroomHostPrefab;
    [SerializeField] private GameObject voidBeastPrefab;

    [Header("랭킹모드 몬스터 프리팹")]
    [SerializeField] private GameObject scareCrowPrefab;

    [Header("스폰 위치")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    private int reservedSummonCount;

    public List<Monster> SpawnEncounter(TrainingNodeType nodeType)
    {
        return SpawnEncounter(CreateEncounterPlan(nodeType, ResolveCurrentFloorNumber()));
    }

    public List<Monster> SpawnEncounter(IReadOnlyList<SpawnMonsterType> encounter)
    {
        if (encounter == null || encounter.Count == 0)
        {
            return new List<Monster>();
        }

        List<Monster> spawnedMonsters = new List<Monster>(encounter.Count);

        for (int i = 0; i < encounter.Count; i++)
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

    public List<Monster> SpawnRankingScareCrow()
    {
        List<Monster> spawnedMonsters = new List<Monster>(1);
        Monster scareCrow = scareCrowPrefab != null
            ? SpawnMonsterToAvailableSlot(scareCrowPrefab)
            : SpawnRuntimeScareCrow();

        if (scareCrow != null)
        {
            spawnedMonsters.Add(scareCrow);
        }

        return spawnedMonsters;
    }

    public static List<SpawnMonsterType> CreateEncounterPlan(TrainingNodeType nodeType, int floorNumber)
    {
        SpawnMonsterType[] encounter = ResolveEncounter(nodeType, floorNumber);
        return new List<SpawnMonsterType>(encounter);
    }

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

    public Monster SpawnSummonedMonster(GameObject monsterPrefab)
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] 소환할 몬스터 프리팹이 비어 있습니다");
            return null;
        }

        return SpawnMonsterToAvailableSlot(monsterPrefab);
    }

    public bool TryReserveSummonSlot()
    {
        if (GetRemainingSummonCapacity() <= 0)
        {
            return false;
        }

        reservedSummonCount++;
        return true;
    }

    public void ResetSummonReservations()
    {
        reservedSummonCount = 0;
    }

    private static SpawnMonsterType[] ResolveEncounter(TrainingNodeType nodeType, int floorNumber)
    {
        return PickRandomEncounter(ResolveEncounterTable(nodeType, floorNumber));
    }

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

    private static SpawnMonsterType[] PickRandomEncounter(SpawnMonsterType[][] encounterTable)
    {
        if (encounterTable == null || encounterTable.Length == 0)
        {
            return new SpawnMonsterType[0];
        }

        return encounterTable[UnityEngine.Random.Range(0, encounterTable.Length)];
    }

    private static bool IsLateGameEncounterFloor(int floorNumber)
    {
        return floorNumber >= 9;
    }

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

    private Monster SpawnMonsterToAvailableSlot(GameObject monsterPrefab)
    {
        if (!TryGetAvailableSpawnPoint(out Transform spawnPoint))
        {
            Debug.LogWarning($"[MonsterSpawner] 남은 스폰 위치가 없어 '{monsterPrefab.name}' 생성이 취소되었습니다");
            return null;
        }

        GameObject spawnedObject = Instantiate(monsterPrefab, spawnPoint, false);
        spawnedObject.transform.localPosition = Vector3.zero;
        spawnedObject.transform.localRotation = Quaternion.identity;
        spawnedObject.transform.localScale = Vector3.one;

        if (!spawnedObject.TryGetComponent(out Monster monster))
        {
            Debug.LogError($"[MonsterSpawner] 생성된 프리팹 '{spawnedObject.name}'에 Monster 컴포넌트가 없습니다");
            Destroy(spawnedObject);
            return null;
        }

        return monster;
    }

    private Monster SpawnRuntimeScareCrow()
    {
        if (!TryGetAvailableSpawnPoint(out Transform spawnPoint))
        {
            Debug.LogWarning("[MonsterSpawner] 남은 스폰 위치가 없어 허수아비 생성이 취소되었습니다");
            return null;
        }

        Debug.LogWarning("[MonsterSpawner] 허수아비 프리팹이 없어 런타임 허수아비를 생성합니다");

        GameObject scareCrowObject = new GameObject(
            "ScareCrow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ScareCrowMonster));
        scareCrowObject.transform.SetParent(spawnPoint, false);
        scareCrowObject.transform.localPosition = Vector3.zero;
        scareCrowObject.transform.localRotation = Quaternion.identity;
        scareCrowObject.transform.localScale = Vector3.one;

        RectTransform rectTransform = scareCrowObject.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(180f, 240f);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        Image image = scareCrowObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color32(190, 150, 85, 255);
        }

        AddRuntimeScareCrowLabel(scareCrowObject.transform);
        return scareCrowObject.GetComponent<ScareCrowMonster>();
    }

    private static void AddRuntimeScareCrowLabel(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        GameObject labelObject = new GameObject("ScareCrowLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform labelRect = labelObject.transform as RectTransform;
        if (labelRect != null)
        {
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 18f);
            labelRect.sizeDelta = new Vector2(0f, 40f);
        }

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = "허수아비";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 24f;
            label.color = Color.black;
        }
    }

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
            case SpawnMonsterType.MutantSweetPotato:
                return mutantSweetPotatoPrefab;
            case SpawnMonsterType.MutantCarrot:
                return mutantCarrotPrefab;
            case SpawnMonsterType.Mirror:
                return mirrorPrefab;
            case SpawnMonsterType.Cactus:
                return cactusPrefab;
            case SpawnMonsterType.WoodenPuppet:
                return woodenPuppetPrefab;
            case SpawnMonsterType.VoidBug:
                return voidBugPrefab;
            case SpawnMonsterType.JackORipper:
                return jackORipperPrefab;
            case SpawnMonsterType.GiantFlowerSpider:
                return giantFlowerSpiderPrefab;
            case SpawnMonsterType.Prophet:
                return prophetPrefab;
            case SpawnMonsterType.IceAndFireBoss:
                return iceAndFireBossPrefab;
            case SpawnMonsterType.VoidLordBoss:
                return voidLordBossPrefab;
            case SpawnMonsterType.StoneShieldGolem:
                return stoneShieldGolemPrefab;
            case SpawnMonsterType.StoneThrowGolem:
                return stoneThrowGolemPrefab;
            case SpawnMonsterType.StoneStealGolem:
                return stoneStealGolemPrefab;
            case SpawnMonsterType.HauntedCloth:
                return hauntedClothPrefab;
            case SpawnMonsterType.FireSpirit:
                return fireSpiritPrefab;
            case SpawnMonsterType.RotwoodWarden:
                return rotwoodWardenPrefab;
            case SpawnMonsterType.Priestess:
                return priestessPrefab;
            case SpawnMonsterType.MushroomHost:
                return mushroomHostPrefab;
            case SpawnMonsterType.VoidBeast:
                return voidBeastPrefab;
            default:
                return null;
        }
    }
}
