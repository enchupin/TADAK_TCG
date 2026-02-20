using UnityEngine;

/// <summary>
/// 처형 공격 효과
/// 적 HP가 특정 비율 이하일 때 데미지가 증가합니다./// </summary>
public class ExecuteDamageEffect : ICardEffect
{
    public int baseDamage;
    public float hpThreshold = 0.5f; // 기본값 50%
    public float multiplier = 2.0f;  // 기본값 2배
    
    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager.spawnedMonsters.Count == 0) return;
        
        Monster monster = battleManager.spawnedMonsters[0];
        if (monster == null) return;
        
        PlayerData player = battleManager.playerData;
        float enemyHpPercent = (float)monster.hp / monster.maxHP;
        
        int finalDamage = baseDamage;
        if (enemyHpPercent <= hpThreshold)
        {
            finalDamage = Mathf.RoundToInt(baseDamage * multiplier);
            Debug.Log($"처형! 적 HP {enemyHpPercent * 100}% → 데미지 {multiplier}배!");
        }
        // 이거 복구하다가 대충 적었는데 TakeDamage()의 인자가 왜 2개인지 모르겠음 맞는지 확인 부탁드립니다
        // 확인해보니까 TakeDamage가 계산된 값을 인자로 받는게 아니라
        // 카드의 데미지 + 플레이어 버프를 인자로 받아서 내부적으로 최종 데미지를 계산해서 사용하는
        // 방식인 것 같습니다
        monster.TakeDamage(finalDamage, player.strength);
    }
}
