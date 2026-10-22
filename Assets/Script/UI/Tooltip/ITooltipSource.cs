/// <summary>
/// 호버 대상이 현재 표시할 툴팁 문구를 돌려줍니다
/// </summary>
// 표시 중에는 매 프레임 다시 조회하므로 버프 중첩 같은 수치 변화가 바로 반영됨
// 비어 있는 문자열을 돌려주면 툴팁을 숨김
public interface ITooltipSource
{
    string GetTooltipText();
}
