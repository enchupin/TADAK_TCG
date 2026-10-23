using TMPro;
using UnityEngine;

/// <summary>
/// 실행 중 생성되는 프리팹의 TMP 텍스트에 현재 언어 폰트 적용
/// </summary>
// 자식 TMP 텍스트 전체에 적용, 프로필(LocalizedTMPFontProfile)이 관리하는 폰트를 쓰는 텍스트만 바꿈
[DisallowMultipleComponent]
public sealed class LocalizedFont : MonoBehaviour
{
    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Apply;
        Apply();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Apply;
    }

    public void Apply()
    {
        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            LocalizedFontApplier.Apply(text);
        }
    }
}
