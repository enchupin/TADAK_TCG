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
    /*
    public void Execute(PlayerData player, Monster target, int cardsPlayedThisTurn = 0)
    {
        int totalDamage = baseDamage + (bonusPerCard * cardsPlayedThisTurn);
        target.TakeDamage(totalDamage, player.strength);
        
        Debug.Log($"연계 공격! 기본 {baseDamage} + ({bonusPerCard} x {cardsPlayedThisTurn}) = {totalDamage}");
    }

    // 인자를 2개만 전달받았다면 기본적으로 0으로 처리
    public void Execute(PlayerData player, Monster target) {
        int cardsPlayedThisTurn = 0;
        int totalDamage = baseDamage + (bonusPerCard * cardsPlayedThisTurn);
        target.TakeDamage(totalDamage, player.strength);

        Debug.Log($"연계 공격! 기본 {baseDamage} + ({bonusPerCard} x {cardsPlayedThisTurn}) = {totalDamage}");
    }
    */


    public void Execute(TrainingBattleManager battleManager) {
    }

}
