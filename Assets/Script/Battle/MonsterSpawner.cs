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
    
    [Tooltip("몬스터가 생성될 위치")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// 몬스터 프리팹을 지정된 위치에 스폰
    /// </summary>
    /// <returns>생성된 몬스터 객체 (없으면 null)</returns>
    public Monster SpawnMonster()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("[MonsterSpawner] 스폰할 몬스터 프리팹이 설정되지 않았습니다!");
            return null;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject spawnedObj;
        if (spawnPoint != null) {
            // spawnPoint를 부모로 설정
            spawnedObj = Instantiate(monsterPrefab, spawnPosition, spawnRotation, spawnPoint);
        }
        else {
            spawnedObj = Instantiate(monsterPrefab, spawnPosition, spawnRotation);
        }
        if (!spawnedObj.TryGetComponent<Monster>(out var monsterComponent)) {
            Debug.LogError($"[MonsterSpawner] 생성된 프리팹 '{spawnedObj.name}'에 Monster 컴포넌트가 없습니다!");
        }

        return monsterComponent;
    }
}
