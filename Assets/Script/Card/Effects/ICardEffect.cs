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





    /// <summary>
    /// 효과를 실행합니다. (턴 정보 포함)
    /// 기본적으로는 턴 정보를 무시하고 일반 Execute를 호출합니다.
    /// </summary>
    // 기본적으로는 cardsPlayedThisTurn 정보를 소실시키지만
    // ExecuteDamageEffect 등 정보가 필요한 곳에서는 Execute를 오버라이딩하여 사용
    void Execute(PlayerData player, Monster target, int cardsPlayedThisTurn)
    {
        Execute(player, target);
    }






}
