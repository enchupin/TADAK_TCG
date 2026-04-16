using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizedTMPFontProfile", menuName = "Localization/TMP Font Profile")]
public class LocalizedTMPFontProfile : ScriptableObject
{
    [Header("Default Font")]
    [SerializeField] private TMP_FontAsset defaultFont;

    [Header("Per Language Fonts")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset japaneseFont;
    [SerializeField] private TMP_FontAsset chineseTraditionalFont;
    [SerializeField] private TMP_FontAsset chineseSimplifiedFont;
    [SerializeField] private TMP_FontAsset russianFont;

    public TMP_FontAsset GetFont(LocalizationLanguage language)
    {
        TMP_FontAsset localizedFont = language switch
        {
            LocalizationLanguage.Korean => koreanFont,
            LocalizationLanguage.English => englishFont,
            LocalizationLanguage.Japanese => japaneseFont,
            LocalizationLanguage.ChineseTraditional => chineseTraditionalFont,
            LocalizationLanguage.ChineseSimplified => chineseSimplifiedFont,
            LocalizationLanguage.Russian => russianFont,
            _ => null
        };

        if (localizedFont != null)
        {
            return localizedFont;
        }

        return defaultFont;
    }
}
