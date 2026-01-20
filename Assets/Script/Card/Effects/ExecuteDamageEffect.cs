using UnityEngine;

/// <summary>
/// 처형 공격 효과
/// 적 HP가 특정 비율 이하일 때 데미지가 증가합니다.
/// </summary>
public class ExecuteDamageEffect : ICardEffect
{
    public int baseDamage;
    public float hpThreshold = 0.5f; // 기본값 50%
    public float multiplier = 2.0f;  // 기본값 2배
    
    public void Execute(BattleContext context)
    {
        float enemyHpPercent = (float)context.enemyHP / context.enemyMaxHP;
        
        int finalDamage = baseDamage;
        if (enemyHpPercent <= hpThreshold)
        {
            finalDamage = Mathf.RoundToInt(baseDamage * multiplier);
            Debug.Log($"처형! 적 HP {enemyHpPercent * 100}% → 데미지 {multiplier}배!");
        }
        
        context.DealDamage(finalDamage);
    }
}
