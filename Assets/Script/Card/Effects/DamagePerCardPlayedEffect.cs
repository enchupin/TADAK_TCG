using UnityEngine;

/// <summary>
/// 연계 공격 효과
/// 이번 턴에 낸 카드 수에 비례하여 데미지가 증가합니다.
/// 주의: 이 효과는 BattleContext가 필요하므로 턴 기반 모드에서만 사용 가능합니다.
/// </summary>
public class DamagePerCardPlayedEffect : ICardEffect
{
    public int baseDamage;
    public int bonusPerCard;
    
    // 턴 정보를 저장하기 위한 필드 (BattleContext에서 설정)
    [System.NonSerialized]
    public int cardsPlayedThisTurn = 0;
    
    public void Execute(PlayerData player, Monster target)
    {
        int totalDamage = baseDamage + (bonusPerCard * cardsPlayedThisTurn);
        target.TakeDamage(totalDamage, player.strength);
        
        Debug.Log($"연계 공격! 기본 {baseDamage} + ({bonusPerCard} x {cardsPlayedThisTurn}) = {totalDamage}");
    }
}
