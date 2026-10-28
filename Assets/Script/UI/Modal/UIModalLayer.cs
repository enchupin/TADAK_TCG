using System;
using System.Collections.Generic;

// 패널 종류: 일반은 ESC로 닫힘, 필수는 선택을 마칠 때까지 ESC 무시
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
    // 맨 위 패널 닫기
    CloseTop,
    // 맨 위 패널이 필수라 무시
    Ignore,
    // 열린 패널이 없어 씬 기본 동작 (전투 = 설정 열기 등)
    SceneDefault
}

public sealed class UIModalEntry
{
    public object Owner { get; }
    // 같은 패널을 보기 · 선택으로 번갈아 쓰므로 열린 상태에서도 바뀔 수 있음
    public UIModalKind Kind { get; set; }
    // 나중에 열린 패널보다도 항상 위에 둠 (설정)
    public bool AlwaysOnTop { get; }
    // 맨 위일 때 전투 화면 툴팁(버프 · 의도) 허용 (몬스터 직접 선택)
    public bool AllowBattleTooltips { get; }
    public Action Close { get; }

    public UIModalEntry(object owner, UIModalKind kind, bool alwaysOnTop, bool allowBattleTooltips, Action close)
    {
        Owner = owner;
        Kind = kind;
        AlwaysOnTop = alwaysOnTop;
        AllowBattleTooltips = allowBattleTooltips;
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
    public bool AllowsBattleTooltips => !HasAny || Top.AllowBattleTooltips;

    public IReadOnlyList<UIModalEntry> Entries => entries;

    public bool Contains(object owner)
    {
        return IndexOf(owner) >= 0;
    }

    /// <summary>
    /// 패널 등록. 이미 등록된 패널은 그대로 두고 기존 항목 반환
    /// 항상 위 패널이 아니면 항상 위 패널들 바로 아래에 끼워 넣음
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

        int insertIndex = entries.Count;
        if (!entry.AlwaysOnTop)
        {
            while (insertIndex > 0 && entries[insertIndex - 1].AlwaysOnTop)
            {
                insertIndex--;
            }
        }

        entries.Insert(insertIndex, entry);
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

        UIModalEntry top = Top;
        if (top == null)
        {
            return UIEscapeAction.SceneDefault;
        }

        return top.Kind == UIModalKind.Required ? UIEscapeAction.Ignore : UIEscapeAction.CloseTop;
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
