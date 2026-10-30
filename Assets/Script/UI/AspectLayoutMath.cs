// 화면비 맞춤 계산 (Unity 의존 없음, FixedAspectRectTransform · AspectCoverImage 공용)
public static class AspectLayoutMath
{
    // 부모 안에 비율을 유지한 채 들어가는 가장 큰 크기 (16:9 영역 고정)
    public static void Contain(float parentWidth, float parentHeight, float aspect, out float width, out float height)
    {
        width = parentWidth;
        height = parentHeight;
        if (parentWidth <= 0f || parentHeight <= 0f || aspect <= 0f)
        {
            return;
        }

        if (parentWidth / parentHeight > aspect)
        {
            width = parentHeight * aspect;
        }
        else
        {
            height = parentWidth / aspect;
        }
    }

    // 비율을 유지한 채 부모를 빈틈없이 덮는 가장 작은 크기 (배경 확대, 넘치는 쪽은 잘림)
    public static void Cover(float parentWidth, float parentHeight, float aspect, out float width, out float height)
    {
        width = parentWidth;
        height = parentHeight;
        if (parentWidth <= 0f || parentHeight <= 0f || aspect <= 0f)
        {
            return;
        }

        if (parentWidth / parentHeight > aspect)
        {
            height = parentWidth / aspect;
        }
        else
        {
            width = parentHeight * aspect;
        }
    }
}
