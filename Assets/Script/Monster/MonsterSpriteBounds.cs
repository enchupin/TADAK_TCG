using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 몬스터 이미지의 그림 영역(투명 여백 제외) 측정 데이터
/// 좌표는 Unity 텍스처와 같이 좌하단 원점, 픽셀 단위
/// 에디터 측정 도구 · 검증 테스트 · 런타임 배치가 함께 쓰도록 Unity 의존 없이 작성
/// </summary>
[Serializable]
public class MonsterSpriteBoundsData
{
    public const string ResourcePath = "JsonData/monsterSpriteBounds";
    public const int CurrentVersion = 1;
    // 이 값 이상인 알파만 그림으로 취급 (가장자리 흐림 무시)
    public const int DefaultAlphaThreshold = 16;

    public int version = CurrentVersion;
    public int alphaThreshold = DefaultAlphaThreshold;
    public List<MonsterSpriteBoundsEntry> images = new();

    public MonsterSpriteBoundsEntry Find(string resourcePath)
    {
        if (images == null || string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        foreach (MonsterSpriteBoundsEntry entry in images)
        {
            if (entry != null && entry.path == resourcePath)
            {
                return entry;
            }
        }

        return null;
    }

    // 이미지 한 장당 한 줄로 써서 변경 내역을 보기 쉽게 함
    public string ToJson()
    {
        StringBuilder builder = new();
        builder.Append("{\n");
        builder.Append($"  \"version\": {version},\n");
        builder.Append($"  \"alphaThreshold\": {alphaThreshold},\n");
        builder.Append("  \"images\": [\n");
        for (int i = 0; i < images.Count; i++)
        {
            MonsterSpriteBoundsEntry e = images[i];
            builder.Append(string.Format(CultureInfo.InvariantCulture,
                "    {{ \"path\": \"{0}\", \"width\": {1}, \"height\": {2}, \"x\": {3}, \"y\": {4}, \"w\": {5}, \"h\": {6} }}",
                e.path, e.width, e.height, e.x, e.y, e.w, e.h));
            builder.Append(i < images.Count - 1 ? ",\n" : "\n");
        }

        builder.Append("  ]\n");
        builder.Append("}\n");
        return builder.ToString();
    }
}

[Serializable]
public class MonsterSpriteBoundsEntry
{
    // Resources 경로 (확장자 제외)
    public string path;
    // 원본 캔버스 크기
    public int width;
    public int height;
    // 그림 영역 (좌하단 원점), 완전히 투명하면 w · h가 0
    public int x;
    public int y;
    public int w;
    public int h;

    public bool IsEmpty => w <= 0 || h <= 0;

    // 하단 투명 여백
    public int BottomPadding => IsEmpty ? 0 : y;

    // alphaAt(x, y)는 좌하단 원점 기준 알파값
    public static MonsterSpriteBoundsEntry Measure(string path, int width, int height, Func<int, int, byte> alphaAt, int alphaThreshold)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (alphaAt(x, y) < alphaThreshold)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        MonsterSpriteBoundsEntry entry = new() { path = path, width = width, height = height };
        if (maxX >= 0)
        {
            entry.x = minX;
            entry.y = minY;
            entry.w = maxX - minX + 1;
            entry.h = maxY - minY + 1;
        }

        return entry;
    }
}
