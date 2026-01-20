using UnityEngine;

/// <summary>
/// 버프 효과
/// 플레이어의 스탯을 증가시킵니다.
/// </summary>
public class BuffEffect : ICardEffect
{
    public string stat; // "Strength", "Dexterity" 등
    public int amount;
    public int duration; // 나중에 턴 기반 버프 구현 시 사용
    
    public void Execute(BattleContext context)
    {
        context.AddBuff(stat, amount);
    }
}
