using UnityEngine;

/// <summary>
/// 드로우 효과
/// 덱에서 카드를 뽑습니다.
/// </summary>
public class DrawEffect : ICardEffect
{
    public int amount;
    
    public void Execute(BattleContext context)
    {
        context.DrawCards(amount);
    }
}
