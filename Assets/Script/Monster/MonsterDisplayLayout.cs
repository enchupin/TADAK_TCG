using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 몬스터 이미지 배치 설정 (JsonData/monsterDisplay.json)
/// 기준 칸 · 지면 높이는 공통, 배율 · 위치 보정은 몬스터 이미지 이름별
/// 상태(Idle · Attack · Hit)별 캔버스가 달라 상태마다 추가 보정 가능 (버전 2)
/// 이미지를 같은 밀도로 다시 만든 뒤 기본 배율을 UI 1단위당 픽셀 수로 고정 (버전 3)
/// 비교 씬 저장 · 검증 테스트가 같은 서식을 쓰도록 Unity 의존 없이 작성
/// </summary>
[Serializable]
public class MonsterDisplayData
{
    public const string ResourcePath = "JsonData/monsterDisplay";
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    // 이미지 픽셀 수 ÷ 이 값 = UI 크기 (1920×1080 기준 화면 1px = UI 1단위)
    public float pixelsPerUnit = 1f;
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
        builder.Append($"  \"pixelsPerUnit\": {Format(pixelsPerUnit)},\n");
        builder.Append($"  \"groundOffset\": {Format(groundOffset)},\n");
        builder.Append($"  \"intentGap\": {Format(intentGap)},\n");
        builder.Append("  \"monsters\": [\n");
        for (int i = 0; i < monsters.Count; i++)
        {
            MonsterDisplayEntry e = monsters[i];
            builder.Append($"    {{ \"image\": \"{e.image}\", {FormatValues(e.scale, e.offsetX, e.offsetY)}, "
                + $"\"idle\": {FormatState(e.idle)}, \"attack\": {FormatState(e.attack)}, \"hit\": {FormatState(e.hit)} }}");
            builder.Append(i < monsters.Count - 1 ? ",\n" : "\n");
        }

        builder.Append("  ]\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string FormatValues(float scale, float offsetX, float offsetY)
    {
        return $"\"scale\": {Format(scale)}, \"offsetX\": {Format(offsetX)}, \"offsetY\": {Format(offsetY)}";
    }

    private static string FormatState(MonsterDisplayStateEntry state)
    {
        state ??= new MonsterDisplayStateEntry();
        return $"{{ {FormatValues(state.scale, state.offsetX, state.offsetY)} }}";
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
    // 상태별 추가 보정 (배율은 곱, 위치는 더함)
    public MonsterDisplayStateEntry idle = new();
    public MonsterDisplayStateEntry attack = new();
    public MonsterDisplayStateEntry hit = new();

    // stateSuffix: MonsterImagePaths의 상태 이름 (Idle · Attack · Hit)
    public MonsterDisplayStateEntry GetState(string stateSuffix)
    {
        switch (stateSuffix)
        {
            case MonsterImagePaths.AttackImageSuffix:
                return attack ??= new MonsterDisplayStateEntry();
            case MonsterImagePaths.HitImageSuffix:
                return hit ??= new MonsterDisplayStateEntry();
            default:
                return idle ??= new MonsterDisplayStateEntry();
        }
    }
}

[Serializable]
public class MonsterDisplayStateEntry
{
    public float scale = 1f;
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
    // 체력 바 폭 기준이 되는 Idle 그림 폭 (상태가 바뀌어도 고정)
    public float idleContentWidth;
}

public static class MonsterSpriteLayout
{
    // current: 현재 상태 이미지 측정값, idle: Idle 이미지 측정값 (의도 위치 기준)
    // stateSuffix: 현재 상태 이름, 상태별 보정 선택용
    public static MonsterSpriteLayoutResult Compute(MonsterSpriteBoundsEntry current, MonsterSpriteBoundsEntry idle,
        MonsterDisplayData settings, MonsterDisplayEntry entry, string stateSuffix = MonsterImagePaths.IdleImageSuffix)
    {
        if (current == null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        idle ??= current;
        float pixelsPerUnit = settings != null && settings.pixelsPerUnit > 0f ? settings.pixelsPerUnit : 1f;
        float groundOffset = settings != null ? settings.groundOffset : 0f;
        float entryScale = entry != null && entry.scale > 0f ? entry.scale : 1f;
        float offsetX = entry?.offsetX ?? 0f;
        float offsetY = entry?.offsetY ?? 0f;

        MonsterDisplayStateEntry state = entry?.GetState(stateSuffix) ?? new MonsterDisplayStateEntry();
        MonsterDisplayStateEntry idleState = entry?.GetState(MonsterImagePaths.IdleImageSuffix) ?? new MonsterDisplayStateEntry();
        float baseScale = entryScale / pixelsPerUnit;
        float scale = baseScale * (state.scale > 0f ? state.scale : 1f);
        float idleScale = baseScale * (idleState.scale > 0f ? idleState.scale : 1f);
        float feet = groundOffset + offsetY + state.offsetY;
        float idleFeet = groundOffset + offsetY + idleState.offsetY;

        MonsterSpriteLayoutResult result = new()
        {
            scale = scale,
            width = current.width * scale,
            height = current.height * scale,
            centerX = offsetX + state.offsetX,
            bottom = feet - current.BottomPadding * scale,
            idleContentTop = idleFeet + (idle.IsEmpty ? idle.height : idle.h) * idleScale,
            idleContentWidth = (idle.IsEmpty ? idle.width : idle.w) * idleScale
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

    // 체력 바 폭: 프리팹 폭이 최소, 그림이 더 넓으면 그림 폭 + 여유, 최대값이 0 이하면 제한 없음
    public static float ResolveHealthBarWidth(float minWidth, float contentWidth, float padding, float maxWidth)
    {
        float width = Math.Max(minWidth, contentWidth + padding);
        if (maxWidth > 0f)
        {
            width = Math.Min(width, Math.Max(minWidth, maxWidth));
        }

        return width;
    }
}
