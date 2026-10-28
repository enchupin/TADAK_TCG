using UnityEngine;

namespace Lobby
{
    public class PanelControl : MonoBehaviour
    {
        private const string ButtonClickSoundPath = "Sound/click5";
        private const float ButtonClickSoundVolumeScale = 1.25f;

        [Header("패널 오브젝트")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject trainingModePanel;
        [SerializeField] private GameObject bossModePanel;
        [UnityEngine.Serialization.FormerlySerializedAs("characterBookPanel")]
        [SerializeField] private GameObject dictionaryPanel;

        private static AudioClip buttonClickSound; // 버튼 클릭음
        private static bool isButtonClickSoundLoadTried;



        private void Awake()
        {
            LoadButtonClickSound();
            RegisterSubPanels();
        }

        // 훈련 · 보스 · 도감은 일반 패널로 등록해 ESC로 메인에 돌아감 (메인에서 ESC는 설정 열기)
        // 전체 화면 패널이라 차단막 없이 등록
        private void RegisterSubPanels()
        {
            foreach (GameObject panel in new[] { trainingModePanel, bossModePanel, dictionaryPanel })
            {
                UIModalPanel.Ensure(panel, new UIModalPanel.Options {
                    useDimmer = false,
                    close = CloseCurrentPanelFromEscape
                });
            }
        }

        private void CloseCurrentPanelFromEscape()
        {
            PlayButtonClickSound();
            CloseCurrentPanel();
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

        public void ShowTrainingModePanel()
        {
            OpenTrainingModePanel();
        }

        private void OpenTrainingModePanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(trainingModePanel, true);
        }

        public void ShowBossModePanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(bossModePanel, true);
        }

        public void ShowDictionaryPanel()
        {
            PlayButtonClickSound();
            SetAllPanels(false);
            SetPanel(dictionaryPanel, true);
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
            SetPanel(trainingModePanel, active);
            SetPanel(bossModePanel, active);
            SetPanel(dictionaryPanel, active);
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
