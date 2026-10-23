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
        [Tooltip("이 언어 폰트용 머터리얼 프리셋 (비우면 폰트 기본 머터리얼)")]
        public Material material;
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

    // 언어별 머터리얼 프리셋, 폰트 교체 시 TMP가 기본 머터리얼로 되돌리는 것을 막기 위해 사용
    // 프리셋을 쓰던 텍스트만 대상 언어 프리셋으로 바꾸고, 폰트 기본 머터리얼을 쓰던 텍스트는 그대로 둠
    [Header("Material Presets")]
    [Tooltip("언어 머터리얼이 없을 때 사용 (defaultFont의 아틀라스와 같아야 함)")]
    public Material defaultMaterial;
    public Material koreanMaterial;
    public Material englishMaterial;
    public Material japaneseMaterial;
    public Material chineseTraditionalMaterial;
    public Material chineseSimplifiedMaterial;
    public Material russianMaterial;

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

    // 언어 폰트를 못 찾아 defaultFont로 대체되면 머터리얼도 defaultMaterial로 맞춤
    public Material GetMaterial(string languageCode)
    {
        string code = (languageCode ?? string.Empty).Trim();
        TMP_FontAsset languageFont = code switch
        {
            "ko" => koreanFont,
            "en" => englishFont,
            "ja" => japaneseFont,
            "zh-Hant" => chineseTraditionalFont,
            "zh-Hans" => chineseSimplifiedFont,
            "ru" => russianFont,
            _ => FindAdditionalFont(code)
        };

        if (languageFont == null)
        {
            return defaultMaterial;
        }

        Material material = code switch
        {
            "ko" => koreanMaterial,
            "en" => englishMaterial,
            "ja" => japaneseMaterial,
            "zh-Hant" => chineseTraditionalMaterial,
            "zh-Hans" => chineseSimplifiedMaterial,
            "ru" => russianMaterial,
            _ => FindAdditionalMaterial(code)
        };

        return material;
    }

    // 이 프로필에 등록된 머터리얼 프리셋인지
    public bool IsManagedMaterial(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material == defaultMaterial || material == koreanMaterial || material == englishMaterial || material == japaneseMaterial
            || material == chineseTraditionalMaterial || material == chineseSimplifiedMaterial || material == russianMaterial)
        {
            return true;
        }

        foreach (LanguageFont entry in additionalFonts)
        {
            if (entry != null && entry.material == material)
            {
                return true;
            }
        }

        return false;
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

    private Material FindAdditionalMaterial(string languageCode)
    {
        foreach (LanguageFont entry in additionalFonts)
        {
            if (entry != null && string.Equals(entry.languageCode?.Trim(), languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return entry.material;
            }
        }

        return null;
    }
}
