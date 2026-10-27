using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 스폰 배치 씬에서 몬스터를 클릭하면 칸 선택, 드래그하면 좌표 이동 (그림 영역 판정은 MonsterImageHitArea가 처리)
/// </summary>
public class SpawnLayoutItem : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private SpawnLayoutPreview owner;
    private int slotIndex;

    public void Bind(SpawnLayoutPreview previewOwner, int slot)
    {
        owner = previewOwner;
        slotIndex = slot;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.SelectSlot(slotIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        owner?.DragSlot(slotIndex, eventData.delta);
    }
}
