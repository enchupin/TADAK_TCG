using System;
using UnityEngine;

/// <summary>
/// 기본 몬스터 프리팹에 몬스터 전용 컴포넌트를 조합해 생성합니다
/// </summary>
[Serializable]
public class MonsterPrefabFactory
{
    [Header("기본 몬스터 프리팹")]
    [SerializeField] private GameObject baseMonsterPrefab;

    public Monster CreateMonster(MonsterSpawner.SpawnMonsterType monsterType, Transform parent)
    {
        Type monsterComponentType = ResolveMonsterComponentType(monsterType);
        if (monsterComponentType == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 몬스터 타입에 대응되는 컴포넌트가 없습니다. type={monsterType}");
            return null;
        }

        return CreateMonster(monsterComponentType, parent);
    }

    public Monster CreateMonster(Type monsterComponentType, Transform parent)
    {
        if (baseMonsterPrefab == null)
        {
            Debug.LogError("[MonsterPrefabFactory] 기본 몬스터 프리팹이 설정되지 않았습니다");
            return null;
        }

        if (monsterComponentType == null || !typeof(Monster).IsAssignableFrom(monsterComponentType) || monsterComponentType.IsAbstract)
        {
            Debug.LogError($"[MonsterPrefabFactory] 생성할 수 없는 몬스터 컴포넌트입니다. type={monsterComponentType}");
            return null;
        }

        GameObject spawnedObject = UnityEngine.Object.Instantiate(baseMonsterPrefab, parent, false);
        spawnedObject.name = monsterComponentType.Name;
        spawnedObject.transform.localPosition = Vector3.zero;
        spawnedObject.transform.localRotation = Quaternion.identity;
        spawnedObject.transform.localScale = Vector3.one;

        if (spawnedObject.GetComponent<Monster>() != null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 기본 몬스터 프리팹에는 Monster 하위 컴포넌트를 미리 넣지 않아야 합니다. prefab={baseMonsterPrefab.name}");
            UnityEngine.Object.Destroy(spawnedObject);
            return null;
        }

        MonsterStateController stateController = spawnedObject.GetComponent<MonsterStateController>();
        if (stateController == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 기본 몬스터 프리팹에 MonsterStateController가 없습니다. prefab={baseMonsterPrefab.name}");
            UnityEngine.Object.Destroy(spawnedObject);
            return null;
        }

        Monster monster = spawnedObject.AddComponent(monsterComponentType) as Monster;
        if (monster == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 몬스터 컴포넌트 추가에 실패했습니다. type={monsterComponentType.Name}");
            UnityEngine.Object.Destroy(spawnedObject);
            return null;
        }

        monster.SetStateController(stateController);
        spawnedObject.name = monster.name;
        return monster;
    }

    private static Type ResolveMonsterComponentType(MonsterSpawner.SpawnMonsterType monsterType)
    {
        switch (monsterType)
        {
            case MonsterSpawner.SpawnMonsterType.MutantFlower:
                return typeof(MutantFlowerMonster);
            case MonsterSpawner.SpawnMonsterType.MutantCarnivorousPlant:
                return typeof(MutantCarnivorousPlantMonster);
            case MonsterSpawner.SpawnMonsterType.MutantMushroom:
                return typeof(MutantMushroomMonster);
            case MonsterSpawner.SpawnMonsterType.MutantSweetPotato:
                return typeof(MutantSweetPotatoMonster);
            case MonsterSpawner.SpawnMonsterType.MutantCarrot:
                return typeof(MutantCarrotMonster);
            case MonsterSpawner.SpawnMonsterType.Mirror:
                return typeof(MirrorMonster);
            case MonsterSpawner.SpawnMonsterType.Cactus:
                return typeof(CactusMonster);
            case MonsterSpawner.SpawnMonsterType.WoodenPuppet:
                return typeof(WoodenPuppetMonster);
            case MonsterSpawner.SpawnMonsterType.VoidBug:
                return typeof(VoidBugMonster);
            case MonsterSpawner.SpawnMonsterType.JackORipper:
                return typeof(JackORipperMonster);
            case MonsterSpawner.SpawnMonsterType.FlashyScythe:
                return typeof(FlashyScytheMonster);
            case MonsterSpawner.SpawnMonsterType.GiantFlowerSpider:
                return typeof(GiantFlowerSpiderBossMonster);
            case MonsterSpawner.SpawnMonsterType.Prophet:
                return typeof(ProphetBossMonster);
            case MonsterSpawner.SpawnMonsterType.IceAndFireBoss:
                return typeof(IceAndFireBossMonster);
            case MonsterSpawner.SpawnMonsterType.VoidLordBoss:
                return typeof(VoidLordBossMonster);
            case MonsterSpawner.SpawnMonsterType.StoneShieldGolem:
                return typeof(StoneShieldGolemMonster);
            case MonsterSpawner.SpawnMonsterType.StoneThrowGolem:
                return typeof(StoneThrowGolemMonster);
            case MonsterSpawner.SpawnMonsterType.StoneStealGolem:
                return typeof(StoneStealGolemMonster);
            case MonsterSpawner.SpawnMonsterType.HauntedCloth:
                return typeof(HauntedClothMonster);
            case MonsterSpawner.SpawnMonsterType.FireSpirit:
                return typeof(FireSpiritMonster);
            case MonsterSpawner.SpawnMonsterType.RotwoodWarden:
                return typeof(RotwoodWardenMonster);
            case MonsterSpawner.SpawnMonsterType.Priestess:
                return typeof(PriestessMonster);
            case MonsterSpawner.SpawnMonsterType.MushroomHost:
                return typeof(MushroomHostMonster);
            case MonsterSpawner.SpawnMonsterType.VoidBeast:
                return typeof(VoidBeastMonster);
            default:
                return null;
        }
    }
}
