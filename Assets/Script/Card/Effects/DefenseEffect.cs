using UnityEngine;

/// <summary>
/// 방어 효과
/// 플레이어에게 방어력을 추가합니다.
/// </summary>
public class DefenseEffect : ICardEffect
{
    public int amount;
    
    public void Execute(BattleManager battleManager)
    {
        // player.AddDefense(amount);
    }
}
