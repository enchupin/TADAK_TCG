using System;

// 기준 영역을 기준으로 툴팁을 놓을 방향
public enum TooltipPlacement
{
    // 기준 영역 하단, 좌측 맞춤 (버프)
    Bottom,
    // 기준 영역 상단, 가로 중앙 맞춤 (의도 아이콘 목록)
    Top
}

// 같은 부모 좌표계의 사각형 (y는 위쪽이 큼)
public readonly struct TooltipRect
{
    public readonly float XMin;
    public readonly float YMin;
    public readonly float XMax;
    public readonly float YMax;

    public TooltipRect(float xMin, float yMin, float xMax, float yMax)
    {
        XMin = Math.Min(xMin, xMax);
        YMin = Math.Min(yMin, yMax);
        XMax = Math.Max(xMin, xMax);
        YMax = Math.Max(yMin, yMax);
    }
}

/// <summary>
/// 툴팁 고정 위치 계산 (Unity 의존 없음)
/// </summary>
public static class TooltipLayout
{
    // 툴팁 좌측 상단 좌표를 돌려줌
    // 화면을 벗어나면 방향은 유지하고 화면 안으로 밀어 넣기만 함
    public static (float x, float y) ResolveTopLeft(
        TooltipPlacement placement,
        TooltipRect anchor,
        float width,
        float height,
        float spacing,
        TooltipRect bounds,
        float margin)
    {
        float left;
        float top;
        switch (placement)
        {
            case TooltipPlacement.Top:
                left = (anchor.XMin + anchor.XMax) * 0.5f - width * 0.5f;
                top = anchor.YMax + spacing + height;
                break;
            default:
                left = anchor.XMin;
                top = anchor.YMin - spacing;
                break;
        }

        // 툴팁이 화면보다 크면 좌측 · 상단을 우선 보이게 함
        float maxLeft = bounds.XMax - margin - width;
        float minLeft = bounds.XMin + margin;
        left = Math.Max(minLeft, Math.Min(left, maxLeft));

        float minTop = bounds.YMin + margin + height;
        float maxTop = bounds.YMax - margin;
        top = Math.Min(maxTop, Math.Max(top, minTop));

        return (left, top);
    }
}
