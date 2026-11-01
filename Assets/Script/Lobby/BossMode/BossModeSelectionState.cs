using System;
using System.Collections.Generic;

/// <summary>
/// 보스 모드 캐릭터 · 덱 선택 규칙 (Unity 의존 없음)
/// 캐릭터를 누르면 저장 덱 선택 팝업, 덱이 하나면 팝업 없이 바로 선택
/// 팝업을 덱 선택 없이 닫으면 그 캐릭터 선택도 취소
/// </summary>
public sealed class BossModeSelectionState
{
    public const int RequiredCharacterCount = 3;

    // 캐릭터를 눌렀을 때 화면이 할 일
    public enum ClickResult
    {
        // 3명을 이미 골라 더 고를 수 없음
        Ignored,
        // 저장 덱 선택 팝업 열기 (PendingCharacter)
        OpenDeckPopup,
        // 덱이 하나라 바로 선택
        SelectedDirectly,
        // 덱이 하나인 선택된 캐릭터를 다시 눌러 선택 해제
        Deselected
    }

    private readonly Dictionary<Character, string> selections = new Dictionary<Character, string>();

    public Character? PendingCharacter { get; private set; }
    public int SelectedCount => selections.Count;
    public IReadOnlyDictionary<Character, string> Selections => selections;

    public bool IsSelected(Character character)
    {
        return selections.ContainsKey(character);
    }

    public bool TryGetDeckId(Character character, out string deckId)
    {
        return selections.TryGetValue(character, out deckId);
    }

    public void Clear()
    {
        selections.Clear();
        PendingCharacter = null;
    }

    /// <summary>
    /// 캐릭터 클릭: deckIds는 그 캐릭터의 저장 덱 ID 목록 (저장 순서)
    /// </summary>
    public ClickResult Click(Character character, IReadOnlyList<string> deckIds)
    {
        if (deckIds == null || deckIds.Count == 0)
        {
            return ClickResult.Ignored;
        }

        bool selected = selections.ContainsKey(character);
        if (!selected && selections.Count >= RequiredCharacterCount)
        {
            return ClickResult.Ignored;
        }

        // 덱이 하나면 덱 선택 과정을 건너뜀: 선택 ↔ 해제 전환
        if (deckIds.Count == 1)
        {
            PendingCharacter = null;
            if (selected)
            {
                selections.Remove(character);
                return ClickResult.Deselected;
            }

            selections[character] = deckIds[0];
            return ClickResult.SelectedDirectly;
        }

        // 이미 덱을 고른 캐릭터도 다시 덱 선택으로 (고른 덱은 팝업 결과로 교체 · 취소)
        PendingCharacter = character;
        return ClickResult.OpenDeckPopup;
    }

    // 팝업에서 덱 선택 완료
    public bool ChooseDeck(string deckId)
    {
        if (!PendingCharacter.HasValue || string.IsNullOrEmpty(deckId))
        {
            return false;
        }

        selections[PendingCharacter.Value] = deckId;
        PendingCharacter = null;
        return true;
    }

    // 팝업을 덱 선택 없이 닫음: 그 캐릭터 선택 취소
    public void CancelPending()
    {
        if (!PendingCharacter.HasValue)
        {
            return;
        }

        selections.Remove(PendingCharacter.Value);
        PendingCharacter = null;
    }

    // 저장 덱이 삭제 · 변경되어 찾을 수 없는 선택 제거
    public void RemoveMissing(Func<Character, string, bool> deckExists)
    {
        if (deckExists == null)
        {
            return;
        }

        foreach (Character character in new List<Character>(selections.Keys))
        {
            if (!deckExists(character, selections[character]))
            {
                selections.Remove(character);
            }
        }
    }

    public bool CanStart(int bossId, Func<int, bool> isValidBoss)
    {
        return selections.Count == RequiredCharacterCount && isValidBoss != null && isValidBoss(bossId);
    }
}
