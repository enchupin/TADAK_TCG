using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor.UI;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Awake()
    {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (settingsPanel != null) return;
    }

    private void Update()
    {
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
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void BGMOnChanged() {
        if (BGMControl.Instance == null) return;
        BGMControl.Instance.SetbgmVolume(bgmSlider.value);
    }

    public void SFXOnChanged() {
        if (SFXControl.Instance == null) return;
        SFXControl.Instance.SetSFXVolume(sfxSlider.value);
    }


}
