using System;
using UnityEngine;

/// <summary>
/// 기본 몬스터 프리팹에 몬스터 전용 컴포넌트를 조합해 생성합니다
/// </summary>
[Serializable]
public class MonsterPrefabFactory
{
    // 기본 몬스터 프리팹
    // 씬에 미리 배치한 프리팹에 몬스터 컴포넌트만 설정

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
        GameObject baseMonsterPrefab = parent != null ? parent.gameObject : null;
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

        GameObject spawnedObject = baseMonsterPrefab;

        foreach (Monster previous in spawnedObject.GetComponents<Monster>())
        {
            if (spawnedObject.activeSelf)
            {
                Debug.LogError($"[MonsterPrefabFactory] 기본 몬스터 프리팹에는 Monster 하위 컴포넌트를 미리 넣지 않아야 합니다. prefab={baseMonsterPrefab.name}");
                return null;
            }
        }

        MonsterStateController stateController = spawnedObject.GetComponent<MonsterStateController>();
        if (stateController == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 기본 몬스터 프리팹에 MonsterStateController가 없습니다. prefab={baseMonsterPrefab.name}");
            return null;
        }

        if (stateController.monsterImage == null)
        {
            Debug.LogError("[MonsterSpawner] 스폰 위치의 몬스터 이미지가 인스펙터에 연결되지 않았습니다");
            return null;
        }
        if (stateController.hpSlider == null || stateController.hpFillImage == null)
        {
            Debug.LogError("[MonsterSpawner] 스폰 위치의 체력 UI가 인스펙터에 연결되지 않았습니다");
            return null;
        }
        if (stateController.buffUI == null)
        {
            Debug.LogError("[MonsterSpawner] 스폰 위치의 버프 UI가 인스펙터에 연결되지 않았습니다");
            return null;
        }

        // 이전 몬스터의 제거 처리가 새 몬스터의 이미지에 영향을 주지 않도록 확인
        // 슬롯의 UI는 유지하고 이전 몬스터 컴포넌트의 연결만 해제
        foreach (Monster previous in spawnedObject.GetComponents<Monster>())
        {
            previous.enabled = false;
            previous.hpSlider = null;
            previous.hpFillImage = null;
            previous.buffUI = null;
            UnityEngine.Object.Destroy(previous);
        }

        spawnedObject.SetActive(true);
        Monster monster = spawnedObject.AddComponent(monsterComponentType) as Monster;
        if (monster == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 몬스터 컴포넌트 추가에 실패했습니다. type={monsterComponentType.Name}");
            return null;
        }

        monster.hpSlider = stateController.hpSlider;
        monster.hpFillImage = stateController.hpFillImage;
        monster.buffUI = stateController.buffUI;
        monster.SetStateController(stateController);
        monster.UpdateUI();
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
