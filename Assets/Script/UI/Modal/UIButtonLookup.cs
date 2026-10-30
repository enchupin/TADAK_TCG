using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인스펙터 연결이 비어 있을 때 OnClick에 연결된 대상 · 함수 이름으로 씬 버튼을 찾음
/// </summary>
public static class UIButtonLookup
{
    public static Button FindByOnClick(Object target, string methodName)
    {
        if (target == null || string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (button.onClick.GetPersistentTarget(i) == target
                    && button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return button;
                }
            }
        }
        return null;
    }
}
