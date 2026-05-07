using UnityEngine;

namespace Lobby
{
    public class PanelControl : MonoBehaviour
    {
        private const string ButtonClickSoundPath = "Sound/click5";
        private const float ButtonClickSoundVolumeScale = 1.25f;

        [Header("패널 오브젝트")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject rankingModePanel;
        [SerializeField] private GameObject trainingModePanel;
        [SerializeField] private GameObject characterBookPanel;
        [SerializeField] private GameObject settingsPanel;

        private static AudioClip buttonClickSound; // 버튼 클릭음
        private static bool isButtonClickSoundLoadTried;



        private void Awake()
        {
            LoadButtonClickSound();
        }

        private void Start()
        {
            ShowMainPanel();
        }

        // ─── 열기 ────────────────────────────────

        public void ShowMainPanel()
        {
            SetAllPanels(false);
            SetPanel(mainPanel, true);
        }

        public void ShowRankingModePanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(rankingModePanel, true);
        }

        public void ShowTrainingModePanel()
        {
            InfiniteMode.SetMode(false);
            OpenTrainingModePanel();
        }

        public void ShowInfiniteModePanel()
        {
            InfiniteMode.SetMode(true);
            OpenTrainingModePanel();
        }

        private void OpenTrainingModePanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(trainingModePanel, true);
        }

        public void ShowCharacterBookPanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(characterBookPanel, true);
        }

        public void ShowSettingsPanel()
        {
            PlayButtonClickSound();
            if (SettingsManager.Instance != null) {
                SettingsManager.Instance.OpenSettingsPanel();
                return;
            }

            SetAllPanels(false);
            SetPanel(settingsPanel, true);
        }

        // ─── 닫기 ────────────────────────────────

        public void CloseCurrentPanel()
        {
            SetAllPanels(false);
            SetPanel(mainPanel, true); // 닫으면 메인 패널로 돌아가기
        }

        // ─── 내부 유틸 ───────────────────────────

        private void SetAllPanels(bool active)
        {
            SetPanel(mainPanel, active);
            SetPanel(rankingModePanel, active);
            SetPanel(trainingModePanel, active);
            SetPanel(characterBookPanel, active);
            SetPanel(settingsPanel, active);
        }

        private void SetPanel(GameObject panel, bool active)
        {
            if (panel == null)
            {
                return;
            }

            if (!active && panel.activeSelf)
            {
                ResetHoverStates(panel);
            }

            panel.SetActive(active);
        }

        private static void ResetHoverStates(GameObject panel)
        {
            UIHoverEffect[] hoverEffects = panel.GetComponentsInChildren<UIHoverEffect>(true);
            foreach (UIHoverEffect hoverEffect in hoverEffects)
            {
                hoverEffect?.ResetHoverState();
            }

            UIHoverEffectAdvanced[] advancedHoverEffects = panel.GetComponentsInChildren<UIHoverEffectAdvanced>(true);
            foreach (UIHoverEffectAdvanced hoverEffect in advancedHoverEffects)
            {
                hoverEffect?.ResetHoverState();
            }
        }

        private static void LoadButtonClickSound()
        {
            if (isButtonClickSoundLoadTried) {
                return;
            }

            isButtonClickSoundLoadTried = true;
            buttonClickSound = Resources.Load<AudioClip>(ButtonClickSoundPath);
            if (buttonClickSound == null) {
                Debug.LogError($"[PanelControl] 버튼 클릭음을 불러오지 못했습니다: {ButtonClickSoundPath}");
            }
        }

        private static void PlayButtonClickSound()
        {
            if (SFXControl.Instance != null) {
                SFXControl.Instance.PlaySFX(buttonClickSound, ButtonClickSoundVolumeScale);
            }
        }
    }
}
