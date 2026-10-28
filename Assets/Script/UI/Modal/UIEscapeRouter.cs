using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// ESC 입력을 한 곳에서 처리 (씬마다 자동 생성, 씬 배치 불필요)
/// 드래그 취소 → 닫을 수 있는 패널 중 가장 최근 것 닫기 → 없으면 설정 열기 (필수 선택은 건너뜀)
/// </summary>
public sealed class UIEscapeRouter : MonoBehaviour
{
    private static UIEscapeRouter instance;
    // 입력 칸이 같은 프레임에 ESC로 먼저 닫혀도 패널이 함께 닫히지 않도록 직전 프레임 상태도 확인
    private bool wasTypingLastFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("[UIEscapeRouter]");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<UIEscapeRouter>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        // 입력 칸(덱 이름 변경 등)에서는 ESC가 입력 취소에 쓰이므로 패널을 닫지 않음
        if (wasTypingLastFrame || IsTypingInInputField())
        {
            return;
        }

        UIModalStack.HandleEscape(
            CardInteractionHandler.IsAnyCardDragging,
            () => CardInteractionHandler.CancelActiveDrag(),
            OpenSettings);
    }

    private void LateUpdate()
    {
        wasTypingLastFrame = IsTypingInInputField();
    }

    private static bool IsTypingInInputField()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null)
        {
            return false;
        }

        TMP_InputField inputField = selected.GetComponent<TMP_InputField>();
        return inputField != null && inputField.isFocused;
    }

    private static void OpenSettings()
    {
        SettingsManager.Instance?.OpenSettingsPanel();
    }
}
