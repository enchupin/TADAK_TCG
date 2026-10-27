using UnityEngine;

/// <summary>
/// 몬스터 이미지의 클릭 · 호버 판정을 그림 영역으로 제한 (투명 여백 제외)
/// Read/Write 텍스처 없이 측정된 그림 범위(사각형)로 판정
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class MonsterImageHitArea : MonoBehaviour, ICanvasRaycastFilter
{
    // 이미지 사각형 안 그림 영역 비율 (좌하단 원점)
    private Rect contentRatio = new Rect(0f, 0f, 1f, 1f);

    public void SetContentRatio(float x, float y, float width, float height)
    {
        contentRatio = new Rect(x, y, width, height);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        RectTransform rectTransform = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        Rect content = new Rect(
            rect.xMin + rect.width * contentRatio.x,
            rect.yMin + rect.height * contentRatio.y,
            rect.width * contentRatio.width,
            rect.height * contentRatio.height);
        return content.Contains(localPoint);
    }
}
