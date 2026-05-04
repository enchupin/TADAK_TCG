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
            return;
        }

        if (EventSystem.current == null) {
            return;
        }

        PointerEventData routedEventData = new PointerEventData(EventSystem.current) {
            scrollDelta = new Vector2(-eventData.scrollDelta.y, 0f)
        };
        scrollRect.OnScroll(routedEventData);
    }
}
