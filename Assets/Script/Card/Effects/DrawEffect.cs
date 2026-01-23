using UnityEngine;

/// <summary>
/// 드로우 효과
/// 덱에서 카드를 뽑습니다.
/// 주의: 이 효과는 BattleContext가 필요하므로 턴 기반 모드에서만 사용 가능합니다.
/// </summary>
public class DrawEffect : ICardEffect
{
    public int amount;
    
    // BattleContext가 필요한 효과이므로 기본 Execute는 경고만 표시
    public void Execute(PlayerData player, Monster target)
    {
        Debug.LogWarning("DrawEffect는 BattleContext가 필요한 효과입니다. 턴 기반 모드에서만 사용 가능합니다.");
    }
    
    /// <summary>
    /// BattleContext와 함께 사용하는 Execute (Card.Play(BattleContext)에서 호출)
    /// </summary>
    public void Execute(BattleContext context)
    {
        // 드로우 구현 (나중에 CardManager와 연동)
        Debug.Log($"카드 {amount}장을 드로우합니다.");
        // TODO: context.deck에서 카드 뽑기 구현
    }
}
