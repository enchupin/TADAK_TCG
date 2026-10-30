using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectWheelRouter : MonoBehaviour, IScrollHandler
{
    [SerializeField] private ScrollRect scrollRect;

    public void Bind(ScrollRect targetScrollRect)
    {
        scrollRect = targetScrollRect;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null || scrollRect == null || scrollRect.content == null) {
            return;
        }
        if (!scrollRect.horizontal || scrollRect.vertical) {
            return;
        }
        if (Mathf.Abs(eventData.scrollDelta.y) <= Mathf.Abs(eventData.scrollDelta.x)) {
            // 가로 입력(터치패드 · 틸트 휠)은 그대로 전달
            scrollRect.OnScroll(eventData);
            return;
        }

        if (EventSystem.current == null) {
            return;
        }

        // 휠을 아래로 굴리면 오른쪽으로 이동 (ScrollRect 기본 동작은 왼쪽)
        PointerEventData routedEventData = new PointerEventData(EventSystem.current) {
            scrollDelta = new Vector2(eventData.scrollDelta.y, 0f)
        };
        scrollRect.OnScroll(routedEventData);
    }
}
