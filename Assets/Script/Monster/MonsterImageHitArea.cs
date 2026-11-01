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

    public Rect ContentRatio => contentRatio;

    public void SetContentRatio(float x, float y, float width, float height)
    {
        contentRatio = new Rect(x, y, width, height);
    }

    // 그림 영역 네 모서리 월드 좌표 (좌하 · 좌상 · 우상 · 우하, GetWorldCorners와 같은 순서)
    public void GetContentWorldCorners(Vector3[] corners)
    {
        RectTransform rectTransform = (RectTransform)transform;
        Rect rect = rectTransform.rect;
        float xMin = rect.xMin + rect.width * contentRatio.x;
        float yMin = rect.yMin + rect.height * contentRatio.y;
        float xMax = xMin + rect.width * contentRatio.width;
        float yMax = yMin + rect.height * contentRatio.height;
        corners[0] = rectTransform.TransformPoint(new Vector3(xMin, yMin, 0f));
        corners[1] = rectTransform.TransformPoint(new Vector3(xMin, yMax, 0f));
        corners[2] = rectTransform.TransformPoint(new Vector3(xMax, yMax, 0f));
        corners[3] = rectTransform.TransformPoint(new Vector3(xMax, yMin, 0f));
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
