using UnityEngine;

/// <summary>
/// 데미지 효과
/// 적에게 데미지를 입힙니다.
/// </summary>
public class DamageEffect : ICardEffect
{
    public int amount;
    public TargetType target = TargetType.SingleEnemy;
    
    public void Execute(BattleContext context)
    {
        switch (target)
        {
            case TargetType.SingleEnemy:
                context.DealDamage(amount);
                break;
            case TargetType.AllEnemies:
                // 나중에 여러 적 지원 시 구현
                context.DealDamage(amount);
                break;
        }
    }
}

/// <summary>
/// 타겟 타입 열거형
/// </summary>
public enum TargetType
{
    SingleEnemy,
    AllEnemies,
    Self,
    AllAllies
}
