using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 언어별 TMP 폰트 지정 (Resources/Localization/LocalizedTMPFontProfile.asset)
/// </summary>
// 기존 에셋의 필드(defaultFont ~ russianFont)를 그대로 읽고, 그 밖의 언어는 additionalFonts에 코드로 추가
// autoApplyToScenes를 켜면 씬 로드 · 언어 변경 때 이 프로필의 폰트를 쓰는 모든 TMP 텍스트를 현재 언어 폰트로 바꿈
[CreateAssetMenu(fileName = "LocalizedTMPFontProfile", menuName = "Localization/TMP Font Profile")]
public sealed class LocalizedTMPFontProfile : ScriptableObject
{
    [Serializable]
    public sealed class LanguageFont
    {
        [Tooltip("언어 코드 (fr, de …)")]
        public string languageCode;
        public TMP_FontAsset font;
    }

    public const string ResourcePath = "Localization/LocalizedTMPFontProfile";

    [Tooltip("언어 폰트가 없을 때 사용")]
    public TMP_FontAsset defaultFont;
    public TMP_FontAsset koreanFont;
    public TMP_FontAsset englishFont;
    public TMP_FontAsset japaneseFont;
    public TMP_FontAsset chineseTraditionalFont;
    public TMP_FontAsset chineseSimplifiedFont;
    public TMP_FontAsset russianFont;
    [Tooltip("위 목록에 없는 언어 (fr, de 등)")]
    public List<LanguageFont> additionalFonts = new();
    [Tooltip("씬 로드 · 언어 변경 시 이 프로필 폰트를 쓰는 TMP 텍스트의 폰트를 자동으로 교체 (기본 꺼짐)")]
    public bool autoApplyToScenes;

    public TMP_FontAsset GetFont(string languageCode)
    {
        TMP_FontAsset font = (languageCode ?? string.Empty).Trim() switch
        {
            "ko" => koreanFont,
            "en" => englishFont,
            "ja" => japaneseFont,
            "zh-Hant" => chineseTraditionalFont,
            "zh-Hans" => chineseSimplifiedFont,
            "ru" => russianFont,
            _ => FindAdditionalFont(languageCode)
        };

        return font != null ? font : defaultFont;
    }

    // 이 프로필이 관리하는 폰트인지 (다른 폰트를 쓰는 텍스트는 건드리지 않음)
    public bool IsManagedFont(TMP_FontAsset font)
    {
        if (font == null)
        {
            return false;
        }

        if (font == defaultFont || font == koreanFont || font == englishFont || font == japaneseFont
            || font == chineseTraditionalFont || font == chineseSimplifiedFont || font == russianFont)
        {
            return true;
        }

        foreach (LanguageFont entry in additionalFonts)
        {
            if (entry != null && entry.font == font)
            {
                return true;
            }
        }

        return false;
    }

    private TMP_FontAsset FindAdditionalFont(string languageCode)
    {
        foreach (LanguageFont entry in additionalFonts)
        {
            if (entry != null && string.Equals(entry.languageCode?.Trim(), languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return entry.font;
            }
        }

        return null;
    }
}
