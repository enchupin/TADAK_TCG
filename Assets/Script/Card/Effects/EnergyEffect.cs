using UnityEngine;

/// <summary>
/// 에너지 효과
/// 플레이어의 에너지를 회복합니다.
/// </summary>
public class EnergyEffect : ICardEffect
{
    public int amount;
    
    public void Execute(BattleContext context)
    {
        context.AddEnergy(amount);
    }
}
