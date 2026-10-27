using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 비교 씬에서 몬스터 클릭 시 해당 몬스터 · 상태 선택 (그림 영역 판정은 MonsterImageHitArea가 처리)
/// </summary>
public class MonsterLineupItem : MonoBehaviour, IPointerClickHandler
{
    private MonsterLineupPreview owner;
    private int rowIndex;
    private int stateIndex;

    public void Bind(MonsterLineupPreview lineupOwner, int row, int state)
    {
        owner = lineupOwner;
        rowIndex = row;
        stateIndex = state;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.Select(rowIndex, stateIndex);
    }
}
