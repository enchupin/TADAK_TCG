using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필수 선택(카드 목록) 중에도 차단막을 통과해 눌려야 하는 전투 화면 버튼
/// 상단 지도 · 덱 · 설정, 하단 뽑을 · 버린 · 소멸 카드 더미 (선택은 전투 화면에 귀속되므로 열고 닫기 허용)
/// </summary>
public static class UIBattleSpaceButtons
{
    private static readonly List<Button> buttons = new List<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        buttons.Clear();
    }

    public static void Register(Button button)
    {
        // 씬이 바뀌며 파괴된 버튼 정리
        buttons.RemoveAll(existing => existing == null);
        if (button != null && !buttons.Contains(button))
        {
            buttons.Add(button);
        }
    }

    public static RectTransform[] GetTargets()
    {
        buttons.RemoveAll(existing => existing == null);
        RectTransform[] targets = new RectTransform[buttons.Count];
        for (int i = 0; i < buttons.Count; i++)
        {
            targets[i] = (RectTransform)buttons[i].transform;
        }
        return targets;
    }
}
