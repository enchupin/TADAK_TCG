using System;
using UnityEngine;

/// <summary>
/// 열린 패널 스택 전역 접근점
/// 패널 등록은 UIModalPanel이 활성 · 비활성에 맞춰 처리, 전투 조작 · 툴팁 · ESC는 여기서 판단
/// </summary>
public static class UIModalStack
{
    private static readonly UIModalLayer layer = new UIModalLayer();

    // 패널이 열리거나 닫힐 때 (전투 버튼 · 손패 상태 갱신용)
    public static event Action Changed;

    public static bool HasOpenModal => layer.HasAny;
    public static bool BlocksBattleInput => layer.BlocksBattleInput;
    public static bool AllowsBattleTooltips => layer.AllowsBattleTooltips;
    public static UIModalLayer Layer => layer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        layer.Clear();
        Changed = null;
    }

    public static bool IsTop(UIModalPanel panel)
    {
        return panel != null && layer.Top != null && ReferenceEquals(layer.Top.Owner, panel);
    }

    /// <summary>
    /// ESC 처리: 드래그 취소 → 닫을 수 있는 패널 중 가장 최근 것 닫기 → 없으면 씬 기본 동작 (필수 항목은 건너뜀)
    /// </summary>
    public static UIEscapeAction HandleEscape(bool isDragging, Action cancelDrag, Action sceneDefault)
    {
        UIEscapeAction action = layer.ResolveEscape(isDragging);
        switch (action)
        {
            case UIEscapeAction.CancelDrag:
                cancelDrag?.Invoke();
                break;
            case UIEscapeAction.CloseLatest:
                layer.LatestClosable.Close?.Invoke();
                break;
            case UIEscapeAction.SceneDefault:
                sceneDefault?.Invoke();
                break;
        }
        return action;
    }

    // 패널 오브젝트가 없는 선택 상태(몬스터 직접 선택 · 지도 미리보기)는 직접 등록
    public static void Register(UIModalEntry entry)
    {
        if (layer.Contains(entry.Owner))
        {
            return;
        }

        layer.Push(entry);
        Changed?.Invoke();
    }

    public static void Unregister(object owner)
    {
        if (layer.Remove(owner))
        {
            Changed?.Invoke();
        }
    }

    internal static void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
