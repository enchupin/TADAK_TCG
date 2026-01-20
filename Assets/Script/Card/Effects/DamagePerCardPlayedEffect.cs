using UnityEngine;

/// <summary>
/// 연계 공격 효과
/// 이번 턴에 낸 카드 수에 비례하여 데미지가 증가합니다.
/// </summary>
public class DamagePerCardPlayedEffect : ICardEffect
{
    public int baseDamage;
    public int bonusPerCard;
    
    public void Execute(BattleContext context)
    {
        int totalDamage = baseDamage + (bonusPerCard * context.cardsPlayedThisTurn);
        context.DealDamage(totalDamage);
        
        Debug.Log($"연계 공격! 기본 {baseDamage} + ({bonusPerCard} x {context.cardsPlayedThisTurn}) = {totalDamage}");
    }
}
