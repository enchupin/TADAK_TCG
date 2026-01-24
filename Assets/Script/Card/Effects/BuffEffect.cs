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
    /*
    public void Execute(PlayerData player, Monster target)
    {
        switch (stat.ToLower())
        {
            case "strength":
            case "힘":
                player.AddStrength(amount);
                break;
            // 나중에 다른 스탯 추가 가능
            default:
                Debug.LogWarning($"알 수 없는 버프 스탯: {stat}");
                break;
        }
    }
    */



    public void Execute(BattleManager battleManager) {
    }

}
