using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalizedTMPFontBinder : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private bool autoCollectTargets = true;
    [SerializeField] private bool includeInactiveTargets = true;
    [SerializeField] private List<TMP_Text> targetTexts = new();

    [Header("Font Profile")]
    [SerializeField] private LocalizedTMPFontProfile fontProfile;

    [Header("Fallback Default Font")]
    [SerializeField] private TMP_FontAsset defaultFont;

    private void Reset()
    {
        CollectTargets();
        CacheDefaultFont();
    }

    private void Awake()
    {
        RefreshTargets();
        CacheDefaultFont();
        ApplyCurrentLanguageFont();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        ApplyCurrentLanguageFont();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
    }

    [ContextMenu("Collect Targets")]
    public void CollectTargets()
    {
        targetTexts.Clear();

        TMP_Text[] foundTargets = GetComponentsInChildren<TMP_Text>(includeInactiveTargets);
        foreach (TMP_Text foundTarget in foundTargets)
        {
            if (foundTarget == null)
            {
                continue;
            }

            targetTexts.Add(foundTarget);
        }
    }

    [ContextMenu("Apply Current Language Font")]
    public void ApplyCurrentLanguageFont()
    {
        ApplyFont(LocalizationManager.GetCurrentLanguage());
    }

    public void ApplyFont(LocalizationLanguage language)
    {
        RefreshTargets();

        TMP_FontAsset resolvedFont = ResolveFont(language);
        if (resolvedFont == null)
        {
            return;
        }

        for (int i = 0; i < targetTexts.Count; i++)
        {
            TMP_Text targetText = targetTexts[i];
            if (targetText == null)
            {
                continue;
            }

            if (targetText.font == resolvedFont)
            {
                continue;
            }

            targetText.font = resolvedFont;
            targetText.ForceMeshUpdate();
        }
    }

    private void HandleLanguageChanged()
    {
        ApplyCurrentLanguageFont();
    }

    private void RefreshTargets()
    {
        if (autoCollectTargets || targetTexts.Count == 0)
        {
            CollectTargets();
        }
    }

    private void CacheDefaultFont()
    {
        if (defaultFont != null)
        {
            return;
        }

        for (int i = 0; i < targetTexts.Count; i++)
        {
            TMP_Text targetText = targetTexts[i];
            if (targetText == null || targetText.font == null)
            {
                continue;
            }

            defaultFont = targetText.font;
            return;
        }
    }

    private TMP_FontAsset ResolveFont(LocalizationLanguage language)
    {
        TMP_FontAsset localizedFont = fontProfile != null
            ? fontProfile.GetFont(language)
            : null;

        if (localizedFont != null)
        {
            return localizedFont;
        }

        return defaultFont;
    }
}
