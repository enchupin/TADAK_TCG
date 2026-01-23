/// <summary>
/// 모든 카드 효과가 구현해야 하는 인터페이스
/// </summary>
public interface ICardEffect
{
    /// <summary>
    /// 효과를 실행합니다.
    /// </summary>
    void Execute(BattleManager battlemanager);


    void Execute(BattleManager battlemanager, int amount) {
        Execute(battlemanager);
    }


}
