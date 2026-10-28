using System;
using System.Collections.Generic;

// 패널 종류: 일반은 ESC로 닫힘, 필수(반드시 선택해야 하는 상황)는 ESC로 닫히지 않고 건너뜀
public enum UIModalKind
{
    Normal,
    Required
}

// ESC 입력 해석 결과
public enum UIEscapeAction
{
    // 진행 중인 카드 드래그 취소에 ESC 소모
    CancelDrag,
    // 닫을 수 있는 패널 중 가장 최근 것 닫기
    CloseLatest,
    // 닫을 수 있는 패널이 없어 씬 기본 동작 (전투 = 설정 열기 등)
    SceneDefault
}

public sealed class UIModalEntry
{
    public object Owner { get; }
    // 같은 패널을 보기 · 선택으로 번갈아 쓰므로 열린 상태에서도 바뀔 수 있음
    public UIModalKind Kind { get; set; }
    // 숨김 버튼으로 잠시 숨긴 상태 (전투 조작은 계속 정지, 툴팁만 허용)
    public bool Hidden { get; set; }
    public Action Close { get; }

    public UIModalEntry(object owner, UIModalKind kind, Action close)
    {
        Owner = owner;
        Kind = kind;
        Close = close;
    }
}

/// <summary>
/// 열린 패널 스택의 순수 로직 (Unity 의존 없음)
/// 등록 순서가 곧 위아래 순서이자 ESC로 닫는 순서
/// </summary>
public sealed class UIModalLayer
{
    // 0번이 맨 아래
    private readonly List<UIModalEntry> entries = new List<UIModalEntry>();

    public int Count => entries.Count;
    public bool HasAny => entries.Count > 0;
    public UIModalEntry Top => entries.Count > 0 ? entries[entries.Count - 1] : null;

    // 패널이 하나라도 열려 있으면 전투 진행 정지
    public bool BlocksBattleInput => HasAny;

    // 툴팁은 열린 패널이 없거나 모두 숨김 상태일 때만 허용
    public bool AllowsBattleTooltips
    {
        get
        {
            foreach (UIModalEntry entry in entries)
            {
                if (!entry.Hidden)
                {
                    return false;
                }
            }
            return true;
        }
    }

    // ESC로 닫을 대상: 필수 항목을 건너뛰고 가장 최근에 열린 일반 패널
    public UIModalEntry LatestClosable
    {
        get
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Kind == UIModalKind.Normal)
                {
                    return entries[i];
                }
            }
            return null;
        }
    }

    public IReadOnlyList<UIModalEntry> Entries => entries;

    public bool Contains(object owner)
    {
        return IndexOf(owner) >= 0;
    }

    /// <summary>
    /// 패널 등록. 이미 등록된 패널은 그대로 두고 기존 항목 반환
    /// </summary>
    public UIModalEntry Push(UIModalEntry entry)
    {
        if (entry == null || entry.Owner == null)
        {
            throw new ArgumentException("패널 항목과 소유자가 필요합니다");
        }

        int existing = IndexOf(entry.Owner);
        if (existing >= 0)
        {
            return entries[existing];
        }

        entries.Add(entry);
        return entry;
    }

    public bool Remove(object owner)
    {
        int index = IndexOf(owner);
        if (index < 0)
        {
            return false;
        }

        entries.RemoveAt(index);
        return true;
    }

    public void Clear()
    {
        entries.Clear();
    }

    public UIEscapeAction ResolveEscape(bool isDragging)
    {
        if (isDragging)
        {
            return UIEscapeAction.CancelDrag;
        }

        return LatestClosable != null ? UIEscapeAction.CloseLatest : UIEscapeAction.SceneDefault;
    }

    private int IndexOf(object owner)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Owner, owner))
            {
                return i;
            }
        }
        return -1;
    }
}
