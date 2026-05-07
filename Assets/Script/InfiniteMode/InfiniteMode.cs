using UnityEngine;

public class InfiniteMode : MonoBehaviour
{
    private const float MonsterScalePerMap = 0.2f;

    public static bool IsInfiniteMode { get; private set; }
    public static int CurrentMapIndex { get; private set; } = 1;
    public static float MonsterStatMultiplier => IsInfiniteMode
        ? 1f + Mathf.Max(0, CurrentMapIndex - 1) * MonsterScalePerMap
        : 1f;

    public void SetInfiniteMode(bool isInfiniteMode)
    {
        SetMode(isInfiniteMode);
    }

    public static void SetMode(bool isInfiniteMode)
    {
        IsInfiniteMode = isInfiniteMode;
        CurrentMapIndex = 1;
        Debug.Log(isInfiniteMode
            ? "[InfiniteMode] 무한모드를 시작합니다"
            : "[InfiniteMode] 무한모드를 해제합니다");
    }

    public static void ResetProgress()
    {
        CurrentMapIndex = 1;
    }

    public static void AdvanceMap()
    {
        if (!IsInfiniteMode)
        {
            return;
        }

        CurrentMapIndex++;
        Debug.Log($"[InfiniteMode] {CurrentMapIndex}번째 맵을 시작합니다. 몬스터 수치 배율: {MonsterStatMultiplier:0.##}");
    }

    public static int ScaleMonsterValue(int baseValue)
    {
        if (!IsInfiniteMode || baseValue <= 0)
        {
            return Mathf.Max(0, baseValue);
        }

        return Mathf.Max(baseValue, Mathf.RoundToInt(baseValue * MonsterStatMultiplier));
    }
}
