/// <summary>
/// 모든 카드 효과가 구현해야 하는 인터페이스
/// </summary>
public interface ICardEffect
{
    /// <summary>
    /// 효과를 실행합니다.
    /// </summary>
    /// <param name="player">플레이어 데이터</param>
    /// <param name="target">타겟 몬스터</param>
    void Execute(PlayerData player, Monster target);
}
