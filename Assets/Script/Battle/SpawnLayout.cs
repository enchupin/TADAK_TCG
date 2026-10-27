using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 조우별 몬스터 스폰 좌표 (JsonData/spawnLayouts.json)
/// 좌표는 MonsterSpawnPositions(전투 캔버스 우측 중앙) 기준 UI 단위
/// slots[i]는 조우 몬스터 목록 i번째 위치, 목록보다 뒤쪽 칸은 소환 자리
/// 조정 도구 · 검증 테스트가 같은 서식을 쓰도록 Unity 의존 없이 작성
/// </summary>
[Serializable]
public class SpawnLayoutData
{
    public const string ResourcePath = "JsonData/spawnLayouts";
    public const int CurrentVersion = 1;
    // 랭킹 모드 허수아비 항목 ID (조우 데이터 밖에서 생성)
    public const string RankingScareCrowId = "RANKING";

    public int version = CurrentVersion;
    public List<SpawnLayoutEntry> layouts = new();

    public SpawnLayoutEntry Find(string id)
    {
        if (layouts == null || string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (SpawnLayoutEntry entry in layouts)
        {
            if (entry != null && entry.id == id)
            {
                return entry;
            }
        }

        return null;
    }

    // 조우 한 개당 한 줄로 써서 변경 내역을 보기 쉽게 함
    public string ToJson()
    {
        StringBuilder builder = new();
        builder.Append("{\n");
        builder.Append($"  \"version\": {version},\n");
        builder.Append("  \"layouts\": [\n");
        for (int i = 0; i < layouts.Count; i++)
        {
            SpawnLayoutEntry e = layouts[i];
            builder.Append($"    {{ \"id\": \"{e.id}\", \"slots\": [ ");
            int slotCount = e.slots?.Count ?? 0;
            for (int s = 0; s < slotCount; s++)
            {
                SpawnSlot slot = e.slots[s] ?? new SpawnSlot();
                builder.Append($"{{ \"x\": {Format(slot.x)}, \"y\": {Format(slot.y)} }}");
                builder.Append(s < slotCount - 1 ? ", " : " ");
            }

            builder.Append("] }");
            builder.Append(i < layouts.Count - 1 ? ",\n" : "\n");
        }

        builder.Append("  ]\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string Format(float value)
    {
        return Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);
    }
}

[Serializable]
public class SpawnLayoutEntry
{
    // 조우 ID (E101 · NM02-1 · BS01) 또는 RankingScareCrowId
    public string id;
    public List<SpawnSlot> slots = new();
}

[Serializable]
public class SpawnSlot
{
    public float x;
    public float y;

    public SpawnSlot()
    {
    }

    public SpawnSlot(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}

public static class SpawnLayoutResolver
{
    // 저장 데이터에는 조우 ID가 없어 몬스터 ID 목록으로 찾음, 순서 일치 우선 → 순서 무시 일치
    public static string FindEncounterId(IReadOnlyList<int> monsters, IReadOnlyList<EncounterDefinition> encounters)
    {
        if (monsters == null || monsters.Count == 0 || encounters == null)
        {
            return null;
        }

        foreach (EncounterDefinition encounter in encounters)
        {
            if (encounter != null && SameOrder(encounter.monsters, monsters))
            {
                return encounter.id;
            }
        }

        foreach (EncounterDefinition encounter in encounters)
        {
            if (encounter != null && SameSet(encounter.monsters, monsters))
            {
                return encounter.id;
            }
        }

        return null;
    }

    // 칸별 좌표, 항목이 없거나 칸이 모자라면 씬에 놓인 기본 좌표 사용
    public static List<SpawnSlot> ResolveSlots(SpawnLayoutEntry entry, IReadOnlyList<SpawnSlot> sceneDefaults)
    {
        int count = sceneDefaults?.Count ?? 0;
        List<SpawnSlot> result = new(count);
        for (int i = 0; i < count; i++)
        {
            SpawnSlot slot = entry?.slots != null && i < entry.slots.Count ? entry.slots[i] : null;
            SpawnSlot source = slot ?? sceneDefaults[i] ?? new SpawnSlot();
            result.Add(new SpawnSlot(source.x, source.y));
        }

        return result;
    }

    // 소환 시 빈 칸을 찾는 순서: 시작 몬스터 다음 칸부터, 다 차면 앞 번호
    public static List<int> SummonSlotOrder(int startCount, int slotCount)
    {
        List<int> order = new(Math.Max(0, slotCount));
        int start = Math.Clamp(startCount, 0, Math.Max(0, slotCount));
        for (int i = start; i < slotCount; i++)
        {
            order.Add(i);
        }

        for (int i = 0; i < start; i++)
        {
            order.Add(i);
        }

        return order;
    }

    private static bool SameOrder(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameSet(IReadOnlyList<int> a, IReadOnlyList<int> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        List<int> sortedA = new(a);
        List<int> sortedB = new(b);
        sortedA.Sort();
        sortedB.Sort();
        return SameOrder(sortedA, sortedB);
    }
}
