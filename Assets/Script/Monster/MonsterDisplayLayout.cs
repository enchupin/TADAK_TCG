using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 몬스터 이미지 배치 설정 (JsonData/monsterDisplay.json)
/// 기준 칸 · 지면 높이는 공통, 배율 · 위치 보정은 몬스터 이미지 이름별
/// 비교 씬 저장 · 검증 테스트가 같은 서식을 쓰도록 Unity 의존 없이 작성
/// </summary>
[Serializable]
public class MonsterDisplayData
{
    public const string ResourcePath = "JsonData/monsterDisplay";
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    // Idle 캔버스를 이 칸에 비율 유지로 맞춘 배율이 기본 배율 (기존 MonsterImage 150×300 칸)
    public float referenceWidth = 150f;
    public float referenceHeight = 300f;
    // 스폰 위치 하단(체력 바 상단 부근)에서 발까지의 높이
    public float groundOffset = 10f;
    // 그림 상단에서 의도 영역 하단까지의 간격
    public float intentGap = 10f;
    public List<MonsterDisplayEntry> monsters = new();

    public MonsterDisplayEntry Find(string imageName)
    {
        if (monsters == null || string.IsNullOrEmpty(imageName))
        {
            return null;
        }

        foreach (MonsterDisplayEntry entry in monsters)
        {
            if (entry != null && entry.image == imageName)
            {
                return entry;
            }
        }

        return null;
    }

    // 몬스터 한 종당 한 줄로 써서 변경 내역을 보기 쉽게 함
    public string ToJson()
    {
        StringBuilder builder = new();
        builder.Append("{\n");
        builder.Append($"  \"version\": {version},\n");
        builder.Append($"  \"referenceWidth\": {Format(referenceWidth)},\n");
        builder.Append($"  \"referenceHeight\": {Format(referenceHeight)},\n");
        builder.Append($"  \"groundOffset\": {Format(groundOffset)},\n");
        builder.Append($"  \"intentGap\": {Format(intentGap)},\n");
        builder.Append("  \"monsters\": [\n");
        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterDisplayEntry e = monsters[i];
            builder.Append($"    {{ \"image\": \"{e.image}\", \"scale\": {Format(e.scale)}, \"offsetX\": {Format(e.offsetX)}, \"offsetY\": {Format(e.offsetY)} }}");
            builder.Append(i < monsters.Count - 1 ? ",\n" : "\n");
        }

        builder.Append("  ]\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string Format(float value)
    {
        return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    }
}

[Serializable]
public class MonsterDisplayEntry
{
    // MonsterImagePaths 기준 이미지 이름 (예: Cactus, ProphetBoss)
    public string image;
    // 기본 배율에 곱하는 값
    public float scale = 1f;
    // 좌우 보정, 비행형 띄움 높이
    public float offsetX;
    public float offsetY;
}

/// <summary>
/// 상태 이미지 한 장의 배치 계산 결과 (UI 단위, 스폰 위치 하단 중앙 기준)
/// </summary>
public struct MonsterSpriteLayoutResult
{
    public float scale;
    public float width;
    public float height;
    public float centerX;
    // 이미지 캔버스 하단 위치 (하단 투명 여백만큼 지면 아래로 내려감)
    public float bottom;
    // 이미지 캔버스 안 그림 영역 비율 (좌하단 원점, 0~1)
    public float contentXRatio;
    public float contentYRatio;
    public float contentWidthRatio;
    public float contentHeightRatio;
    // 의도 영역 기준이 되는 Idle 그림 상단 높이 (상태가 바뀌어도 고정)
    public float idleContentTop;
}

public static class MonsterSpriteLayout
{
    // current: 현재 상태 이미지 측정값, idle: Idle 이미지 측정값 (배율 · 의도 위치 기준)
    public static MonsterSpriteLayoutResult Compute(MonsterSpriteBoundsEntry current, MonsterSpriteBoundsEntry idle,
        MonsterDisplayData settings, MonsterDisplayEntry entry)
    {
        if (current == null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        idle ??= current;
        float referenceWidth = settings != null ? settings.referenceWidth : 150f;
        float referenceHeight = settings != null ? settings.referenceHeight : 300f;
        float groundOffset = settings != null ? settings.groundOffset : 0f;
        float entryScale = entry != null && entry.scale > 0f ? entry.scale : 1f;
        float offsetX = entry?.offsetX ?? 0f;
        float offsetY = entry?.offsetY ?? 0f;

        float scale = Math.Min(referenceWidth / Math.Max(1, idle.width), referenceHeight / Math.Max(1, idle.height)) * entryScale;
        float feet = groundOffset + offsetY;

        MonsterSpriteLayoutResult result = new()
        {
            scale = scale,
            width = current.width * scale,
            height = current.height * scale,
            centerX = offsetX,
            bottom = feet - current.BottomPadding * scale,
            idleContentTop = feet + (idle.IsEmpty ? idle.height : idle.h) * scale
        };

        if (current.IsEmpty)
        {
            result.contentWidthRatio = 1f;
            result.contentHeightRatio = 1f;
        }
        else
        {
            result.contentXRatio = (float)current.x / current.width;
            result.contentYRatio = (float)current.y / current.height;
            result.contentWidthRatio = (float)current.w / current.width;
            result.contentHeightRatio = (float)current.h / current.height;
        }

        return result;
    }
}
