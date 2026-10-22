using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 언어별 TMP 폰트 적용 (LocalizedTMPFontProfile 기준)
/// </summary>
// 프로필의 autoApplyToScenes가 켜져 있으면 씬 로드 · 언어 변경 때 씬의 TMP 텍스트를 일괄 교체
// 실행 중 생성되는 프리팹은 LocalizedFont 컴포넌트를 붙이거나 Apply(text)를 직접 호출
public static class LocalizedFontApplier
{
    private static LocalizedTMPFontProfile profile;
    private static bool profileLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        LocalizationManager.LanguageChanged -= ApplyToLoadedScenes;
        LocalizationManager.LanguageChanged += ApplyToLoadedScenes;
        ApplyToLoadedScenes();
    }

    public static LocalizedTMPFontProfile Profile
    {
        get
        {
            if (!profileLoaded)
            {
                profileLoaded = true;
                profile = Resources.Load<LocalizedTMPFontProfile>(LocalizedTMPFontProfile.ResourcePath);
            }

            return profile;
        }
    }

    // 프로필이 관리하는 폰트를 쓰는 텍스트만 현재 언어 폰트로
    public static void Apply(TMP_Text text)
    {
        LocalizedTMPFontProfile currentProfile = Profile;
        if (text == null || currentProfile == null || !currentProfile.IsManagedFont(text.font))
        {
            return;
        }

        TMP_FontAsset font = currentProfile.GetFont(LocalizationManager.GetCurrentLanguageCode());
        if (font != null && text.font != font)
        {
            text.font = font;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToLoadedScenes();
    }

    private static void ApplyToLoadedScenes()
    {
        if (Profile == null || !Profile.autoApplyToScenes)
        {
            return;
        }

        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            Apply(text);
        }
    }
}
