/// <summary>
/// 모든 카드 효과가 구현해야 하는 인터페이스
/// </summary>
public interface ICardEffect
{
    /// <summary>
    /// 효과를 실행합니다.
    /// </summary>
    /// <param name="context">전투 컨텍스트 (플레이어, 적, 턴 정보 등)</param>
    void Execute(BattleContext context);
}
