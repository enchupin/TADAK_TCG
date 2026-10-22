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

    // monsters.json className → 종 컴포넌트 (빌드 코드 제거에 대비해 타입을 직접 참조)
    private static readonly System.Collections.Generic.Dictionary<string, Type> SpeciesTypes = new System.Collections.Generic.Dictionary<string, Type>
    {
        { nameof(MutantFlowerMonster), typeof(MutantFlowerMonster) },
        { nameof(MutantCarnivorousPlantMonster), typeof(MutantCarnivorousPlantMonster) },
        { nameof(MutantMushroomMonster), typeof(MutantMushroomMonster) },
        { nameof(JackORipperMonster), typeof(JackORipperMonster) },
        { nameof(FlashyScytheMonster), typeof(FlashyScytheMonster) },
        { nameof(StoneShieldGolemMonster), typeof(StoneShieldGolemMonster) },
        { nameof(StoneThrowGolemMonster), typeof(StoneThrowGolemMonster) },
        { nameof(StoneStealGolemMonster), typeof(StoneStealGolemMonster) },
        { nameof(VoidBugMonster), typeof(VoidBugMonster) },
        { nameof(HauntedClothMonster), typeof(HauntedClothMonster) },
        { nameof(FireSpiritMonster), typeof(FireSpiritMonster) },
        { nameof(MutantSweetPotatoMonster), typeof(MutantSweetPotatoMonster) },
        { nameof(MutantCarrotMonster), typeof(MutantCarrotMonster) },
        { nameof(MirrorMonster), typeof(MirrorMonster) },
        { nameof(CactusMonster), typeof(CactusMonster) },
        { nameof(WoodenPuppetMonster), typeof(WoodenPuppetMonster) },
        { nameof(RotwoodWardenMonster), typeof(RotwoodWardenMonster) },
        { nameof(PriestessMonster), typeof(PriestessMonster) },
        { nameof(MushroomHostMonster), typeof(MushroomHostMonster) },
        { nameof(VoidBeastMonster), typeof(VoidBeastMonster) },
        { nameof(GiantFlowerSpiderBossMonster), typeof(GiantFlowerSpiderBossMonster) },
        { nameof(ProphetBossMonster), typeof(ProphetBossMonster) },
        { nameof(IceAndFireBossMonster), typeof(IceAndFireBossMonster) },
        { nameof(VoidLordBossMonster), typeof(VoidLordBossMonster) },
    };

    // v1.0 몬스터 ID(4자리)로 생성: monsters.json의 className 컴포넌트에 정의를 넘겨 생성
    public Monster CreateMonster(int monsterId, Transform parent, int staggerIndex = 0)
    {
        MonsterDefinition definition = MonsterDatabase.Get(monsterId);
        if (definition == null)
        {
            Debug.LogError($"[MonsterPrefabFactory] 몬스터 정의가 없습니다. id={monsterId}");
            return null;
        }

        if (!SpeciesTypes.TryGetValue(definition.className ?? string.Empty, out Type monsterComponentType))
        {
            monsterComponentType = Type.GetType(definition.className ?? string.Empty);
        }

        if (monsterComponentType == null || !typeof(DataMonster).IsAssignableFrom(monsterComponentType))
        {
            Debug.LogError($"[MonsterPrefabFactory] 몬스터 타입에 대응되는 컴포넌트가 없습니다. type={definition.className}");
            return null;
        }

        DataMonster.PendingDefinition = definition;
        DataMonster.PendingStaggerIndex = staggerIndex;
        try
        {
            return CreateMonster(monsterComponentType, parent);
        }
        finally
        {
            DataMonster.PendingDefinition = null;
            DataMonster.PendingStaggerIndex = 0;
        }
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
}
