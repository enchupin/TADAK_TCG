using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 상태를 담는 컨텍스트 클래스
/// 효과 실행 시 필요한 모든 정보를 제공합니다.
/// </summary>
public class BattleContext {
    // 플레이어 정보
    public PlayerData playerData;

    // 몬스터 정보
    public Monster monster;

    // 턴 정보
    public int cardsPlayedThisTurn;
    public List<Card> cardsPlayedThisTurnList = new List<Card>();



    /// <summary>
    /// BattleContext 생성자
    /// </summary>
    public BattleContext(int playerMaxHP = 100, int playerMaxEnergy = 3)
    {
        playerData = new PlayerData(playerMaxHP, playerMaxEnergy);
        // monster는 BattleManager에서 초기화
    }

    /// <summary>
    /// 적에게 데미지를 입힙니다. (플레이어의 힘 버프 적용)
    /// </summary>
    public void DealDamageToEnemy(int amount) {
        if (monster != null)
        {
            monster.TakeDamage(amount, playerData.strength);
        }
        else
        {
            Debug.LogWarning("Monster가 초기화되지 않았습니다!");
        }
    }
}
