using System;

/// <summary>
/// 버프 · 의도 아이콘 목록 배치 계산 (Unity 의존 없음)
/// </summary>
public static class IconRowLayout
{
    // 컨테이너 폭에 들어가는 열 수, 아이콘 하나도 안 들어가면 1
    public static int ResolveColumns(float containerWidth, float cellSize, float spacing)
    {
        if (cellSize <= 0f)
        {
            return 1;
        }

        float step = cellSize + Math.Max(0f, spacing);
        // 부동소수 오차로 딱 맞는 폭에서 한 칸이 빠지지 않도록 여유를 둠
        int columns = (int)Math.Floor((containerWidth + Math.Max(0f, spacing)) / step + 0.0001f);
        return Math.Max(1, columns);
    }

    // 좌측 상단부터 줄바꿈 배치할 때 index번째 아이콘의 좌측 상단 좌표 (x는 오른쪽, y는 위쪽이 큼)
    public static (float x, float y) WrapCellTopLeft(int index, int columns, float cellSize, float spacing)
    {
        int safeColumns = Math.Max(1, columns);
        int safeIndex = Math.Max(0, index);
        float step = cellSize + Math.Max(0f, spacing);
        int column = safeIndex % safeColumns;
        int row = safeIndex / safeColumns;
        return (column * step, -row * step);
    }

    public static int RowCount(int count, int columns)
    {
        if (count <= 0)
        {
            return 0;
        }

        int safeColumns = Math.Max(1, columns);
        return (count + safeColumns - 1) / safeColumns;
    }

    // 한 줄 가운데 정렬 목록의 전체 폭
    public static float CenteredRowWidth(int count, float cellSize, float spacing)
    {
        if (count <= 0)
        {
            return 0f;
        }

        return count * cellSize + (count - 1) * Math.Max(0f, spacing);
    }

    // 한 줄 가운데 정렬 목록에서 index번째 아이콘 중심 x (목록 중심이 0)
    public static float CenteredCellCenterX(int index, int count, float cellSize, float spacing)
    {
        float width = CenteredRowWidth(count, cellSize, spacing);
        return -width * 0.5f + index * (cellSize + Math.Max(0f, spacing)) + cellSize * 0.5f;
    }
}
