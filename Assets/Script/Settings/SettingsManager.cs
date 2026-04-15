using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    private const float ResolutionSnapDelay = 0.15f;
    private const float ResolutionWatchIgnoreDuration = 0.35f;

    private static readonly Vector2Int[] SupportedResolutions =
    {
        new Vector2Int(2560, 1440),
        new Vector2Int(1920, 1080),
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(2560, 1080)
    };

    public static SettingsManager Instance { get; private set; }

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private readonly HashSet<Button> boundResolutionButtons = new();
    private Vector2Int lastObservedResolution;
    private Vector2Int pendingResizeResolution;
    private float pendingResizeChangedAt = -1f;
    private float ignoreResolutionWatchUntil = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BindResolutionButtons();
        InitializeResolutionState();
    }

    private void Update()
    {
        HandleWindowResizeSnap();

        if (Keyboard.current == null) {
            return;
        }
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) {
            return;
        }
        ToggleSettingPanel();
    }

    public void ToggleSettingPanel()
    {
        if (settingsPanel == null) {
            return;
        }

        bool shouldOpen = !settingsPanel.activeSelf;
        if (shouldOpen) {
            SyncSoundSliders();
            BindResolutionButtons();
        }

        settingsPanel.SetActive(shouldOpen);
    }

    private void SyncSoundSliders()
    {
        if (bgmSlider != null && BGMControl.Instance != null) {
            bgmSlider.SetValueWithoutNotify(BGMControl.Instance.GetbgmVolume());
        }

        if (sfxSlider != null && SFXControl.Instance != null) {
            sfxSlider.SetValueWithoutNotify(SFXControl.Instance.GetSFXVolume());
        }
    }

    public void BGMOnChanged()
    {
        if (BGMControl.Instance == null) return;
        BGMControl.Instance.SetbgmVolume(bgmSlider.value);
    }

    public void SFXOnChanged()
    {
        if (SFXControl.Instance == null) return;
        SFXControl.Instance.SetSFXVolume(sfxSlider.value);
    }

    public void SetCardLanguageSystem()
    {
        CardLocalizationManager.UseSystemLanguage();
    }

    public void SetCardLanguageKorean()
    {
        CardLocalizationManager.SetCurrentLanguage(CardLocalizationLanguage.Korean);
    }

    public void SetCardLanguageEnglish()
    {
        CardLocalizationManager.SetCurrentLanguage(CardLocalizationLanguage.English);
    }

    public void SetCardLanguageJapanese()
    {
        CardLocalizationManager.SetCurrentLanguage(CardLocalizationLanguage.Japanese);
    }

    public void SetCardLanguageChineseTraditional()
    {
        CardLocalizationManager.SetCurrentLanguage(CardLocalizationLanguage.ChineseTraditional);
    }

    public void SetCardLanguageChineseSimplified()
    {
        CardLocalizationManager.SetCurrentLanguage(CardLocalizationLanguage.ChineseSimplified);
    }

    private void InitializeResolutionState()
    {
        Vector2Int currentResolution = GetCurrentResolution();
        Vector2Int startupResolution = GetNearestSupportedResolution(currentResolution);

        lastObservedResolution = startupResolution;
        pendingResizeResolution = startupResolution;
        pendingResizeChangedAt = -1f;

        if (Application.isEditor) {
            return;
        }

        if (Screen.fullScreenMode == FullScreenMode.Windowed && currentResolution == startupResolution) {
            return;
        }

        ApplyResolution(startupResolution);
    }

    private void BindResolutionButtons()
    {
        if (settingsPanel == null) {
            return;
        }

        Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons) {
            if (button == null) {
                continue;
            }

            if (boundResolutionButtons.Contains(button)) {
                continue;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null || !TryParseResolutionText(label.text, out Vector2Int resolution)) {
                continue;
            }

            Vector2Int capturedResolution = resolution;
            button.onClick.AddListener(() => ApplyResolution(capturedResolution));
            boundResolutionButtons.Add(button);
        }
    }

    private void HandleWindowResizeSnap()
    {
        if (Application.isEditor) {
            return;
        }

        Vector2Int currentResolution = GetCurrentResolution();

        if (Time.unscaledTime < ignoreResolutionWatchUntil) {
            lastObservedResolution = currentResolution;
            return;
        }

        if (Screen.fullScreenMode != FullScreenMode.Windowed) {
            lastObservedResolution = currentResolution;
            pendingResizeResolution = currentResolution;
            pendingResizeChangedAt = -1f;
            return;
        }

        if (currentResolution != lastObservedResolution) {
            lastObservedResolution = currentResolution;
            pendingResizeResolution = currentResolution;
            pendingResizeChangedAt = Time.unscaledTime;
            return;
        }

        if (pendingResizeChangedAt < 0f) {
            return;
        }

        if (Time.unscaledTime - pendingResizeChangedAt < ResolutionSnapDelay) {
            return;
        }

        pendingResizeChangedAt = -1f;
        Vector2Int snappedResolution = GetNearestSupportedResolution(pendingResizeResolution);
        if (snappedResolution != currentResolution) {
            ApplyResolution(snappedResolution);
        }
    }

    private void ApplyResolution(Vector2Int resolution)
    {
        if (!IsSupportedResolution(resolution)) {
            return;
        }

        ignoreResolutionWatchUntil = Time.unscaledTime + ResolutionWatchIgnoreDuration;
        pendingResizeChangedAt = -1f;
        pendingResizeResolution = resolution;
        lastObservedResolution = resolution;

        if (Screen.fullScreenMode == FullScreenMode.Windowed && GetCurrentResolution() == resolution) {
            return;
        }

        Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
    }

    private static Vector2Int GetCurrentResolution()
    {
        return new Vector2Int(Screen.width, Screen.height);
    }

    private static bool TryParseResolutionText(string text, out Vector2Int resolution)
    {
        resolution = default;
        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        string[] tokens = text.Trim().Split(new[] { ' ', 'x', 'X' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2) {
            return false;
        }

        if (!int.TryParse(tokens[0], out int width) || !int.TryParse(tokens[1], out int height)) {
            return false;
        }

        resolution = new Vector2Int(width, height);
        return IsSupportedResolution(resolution);
    }

    private static bool IsSupportedResolution(Vector2Int resolution)
    {
        foreach (Vector2Int supportedResolution in SupportedResolutions) {
            if (supportedResolution == resolution) {
                return true;
            }
        }

        return false;
    }

    private static Vector2Int GetNearestSupportedResolution(Vector2Int resolution)
    {
        Vector2Int nearestResolution = SupportedResolutions[0];
        int shortestDistance = int.MaxValue;

        foreach (Vector2Int supportedResolution in SupportedResolutions) {
            int widthDistance = supportedResolution.x - resolution.x;
            int heightDistance = supportedResolution.y - resolution.y;
            int distance = (widthDistance * widthDistance) + (heightDistance * heightDistance);
            if (distance >= shortestDistance) {
                continue;
            }

            shortestDistance = distance;
            nearestResolution = supportedResolution;
        }

        return nearestResolution;
    }
}
