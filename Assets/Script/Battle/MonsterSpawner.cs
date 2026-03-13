using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 프리팹을 전투 씬에 스폰하는 클래스.
/// 임시로 TrainingBattleManager가 호출하여 몬스터를 생성합니다.
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("스폰할 몬스터 프리팹")]
    [SerializeField] private GameObject monsterPrefab;

    [Tooltip("랜덤 또는 ID 선택에 사용할 몬스터 프리팹 목록")]
    [SerializeField] private List<GameObject> monsterPrefabs = new();

    [Tooltip("활성화되면 목록에서 랜덤으로 몬스터를 선택")]
    [SerializeField] private bool spawnRandomMonster = true;

    [Tooltip("랜덤이 꺼져 있을 때 스폰할 몬스터 ID")]
    [SerializeField] private int fixedMonsterId;
    
    [Tooltip("몬스터가 생성될 위치")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// 몬스터 프리팹을 지정된 위치에 스폰
    /// </summary>
    /// <returns>생성된 몬스터 객체 (없으면 null)</returns>
    public Monster SpawnMonster()
    {
        GameObject selectedPrefab = ResolveMonsterPrefab();
        if (selectedPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] 스폰할 몬스터 프리팹이 설정되지 않았습니다!");
            return null;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject spawnedObj;
        if (spawnPoint != null) {
            // spawnPoint를 부모로 설정
            spawnedObj = Instantiate(selectedPrefab, spawnPosition, spawnRotation, spawnPoint);
        }
        else {
            spawnedObj = Instantiate(selectedPrefab, spawnPosition, spawnRotation);
        }
        if (!spawnedObj.TryGetComponent<Monster>(out var monsterComponent)) {
            Debug.LogError($"[MonsterSpawner] 생성된 프리팹 '{spawnedObj.name}'에 Monster 컴포넌트가 없습니다!");
        }

        return monsterComponent;
    }

    private GameObject ResolveMonsterPrefab()
    {
        if (!spawnRandomMonster && fixedMonsterId > 0)
        {
            GameObject fixedPrefab = FindMonsterPrefabById(fixedMonsterId);
            if (fixedPrefab != null)
            {
                return fixedPrefab;
            }

            Debug.LogWarning($"[MonsterSpawner] ID {fixedMonsterId}에 해당하는 몬스터 프리팹을 찾지 못했습니다.");
        }

        List<GameObject> candidates = GetConfiguredPrefabs();
        if (candidates.Count > 0)
        {
            if (spawnRandomMonster)
            {
                int randomIndex = Random.Range(0, candidates.Count);
                return candidates[randomIndex];
            }

            return candidates[0];
        }

        return monsterPrefab;
    }

    private GameObject FindMonsterPrefabById(int targetMonsterId)
    {
        foreach (GameObject candidate in GetConfiguredPrefabs())
        {
            if (candidate == null)
            {
                continue;
            }

            Monster candidateMonster = candidate.GetComponent<Monster>();
            if (candidateMonster != null && candidateMonster.MonsterId == targetMonsterId)
            {
                return candidate;
            }
        }

        return null;
    }

    private List<GameObject> GetConfiguredPrefabs()
    {
        List<GameObject> candidates = new List<GameObject>();
        if (monsterPrefabs != null)
        {
            foreach (GameObject candidate in monsterPrefabs)
            {
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (monsterPrefab != null && !candidates.Contains(monsterPrefab))
        {
            candidates.Add(monsterPrefab);
        }

        return candidates;
    }
}
