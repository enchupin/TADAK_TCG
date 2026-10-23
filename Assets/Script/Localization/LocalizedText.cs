using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 텍스트에 로컬라이징 키를 지정해 현재 언어 문구로 표시합니다
/// </summary>
// 씬 · 프리팹의 고정 문구용 (버튼 이름, 제목 등), 코드에서 바꾸는 문구에는 붙이지 않음
// 키는 Localization/<언어>/*.json의 아무 키나 사용 가능 (ui.* · monster.<id>.name 등)
[DisallowMultipleComponent]
public sealed class LocalizedText : MonoBehaviour
{
    [Tooltip("로컬라이징 키 (예: ui.combat.endTurn)")]
    [SerializeField] private string key;

    private TMP_Text tmpText;
    private Text legacyText;

    public string Key => key;

    // 코드에서 키를 바꿀 때
    public void SetKey(string newKey)
    {
        key = newKey;
        Refresh();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (tmpText == null && legacyText == null)
        {
            tmpText = GetComponent<TMP_Text>();
            legacyText = tmpText == null ? GetComponent<Text>() : null;
        }

        string text = GameText.Ui(key.Trim());
        if (tmpText != null)
        {
            tmpText.text = text;
        }
        else if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
}
