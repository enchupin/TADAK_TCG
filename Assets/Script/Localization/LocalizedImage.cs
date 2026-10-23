using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 언어별 스프라이트로 바꾸는 이미지 (글자가 들어간 이미지 · 로고용)
/// </summary>
// 현재 언어 스프라이트가 없으면 폴백 순서(languages.json)대로 찾고, 끝까지 없으면 처음 지정된 스프라이트 사용
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class LocalizedImage : MonoBehaviour
{
    [Serializable]
    public sealed class LanguageSprite
    {
        [Tooltip("언어 코드 (ko, en, ja, zh-Hans, zh-Hant, ru …)")]
        public string languageCode;
        public Sprite sprite;
        [Tooltip("켜면 스프라이트 원본 크기로 맞춤")]
        public bool setNativeSize;
    }

    [SerializeField] private List<LanguageSprite> sprites = new();

    private Image image;
    private Sprite defaultSprite;
    private bool initialized;

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
        if (!initialized)
        {
            image = GetComponent<Image>();
            defaultSprite = image != null ? image.sprite : null;
            initialized = true;
        }

        if (image == null)
        {
            return;
        }

        LanguageSprite entry = FindEntry();
        image.sprite = entry != null ? entry.sprite : defaultSprite;
        if (entry != null && entry.setNativeSize)
        {
            image.SetNativeSize();
        }
    }

    private LanguageSprite FindEntry()
    {
        foreach (string code in LocalizationManager.GetFallbackChain())
        {
            foreach (LanguageSprite entry in sprites)
            {
                if (entry != null && entry.sprite != null && string.Equals(entry.languageCode?.Trim(), code, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        return null;
    }
}
