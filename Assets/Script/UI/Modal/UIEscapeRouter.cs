using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC 입력을 한 곳에서 처리 (씬마다 자동 생성, 씬 배치 불필요)
/// 드래그 취소 → 맨 위 패널 닫기 → 필수 패널이면 무시 → 씬 기본 동작(없으면 설정 열기)
/// </summary>
public sealed class UIEscapeRouter : MonoBehaviour
{
    private static UIEscapeRouter instance;

    // 열린 패널이 없을 때 씬별 기본 동작 (비우면 설정 열기)
    public static Action SceneDefault { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        SceneDefault = null;
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

        UIModalStack.HandleEscape(
            CardInteractionHandler.IsAnyCardDragging,
            () => CardInteractionHandler.CancelActiveDrag(),
            RunSceneDefault);
    }

    private static void RunSceneDefault()
    {
        if (SceneDefault != null)
        {
            SceneDefault();
            return;
        }

        SettingsManager.Instance?.OpenSettingsPanel();
    }
}
