using UnityEngine;

/// <summary>
/// 드로우 효과
/// 덱에서 카드를 뽑습니다.
/// 주의: 이 효과는 BattleContext가 필요하므로 턴 기반 모드에서만 사용 가능합니다.
/// </summary>
public class DrawEffect : ICardEffect
{
    public int amount;
    public void Execute(BattleManager battleManager)
    {
        battleManager.drawCardCount++;
    }
}
