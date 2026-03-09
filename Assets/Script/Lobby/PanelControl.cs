using UnityEngine;

namespace Lobby
{
    public class PanelControl : MonoBehaviour
    {
        [Header("패널 오브젝트")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject rankingModePanel;
        [SerializeField] private GameObject trainingModePanel;
        [SerializeField] private GameObject characterBookPanel;
        [SerializeField] private GameObject settingsPanel;



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
            SetAllPanels(false);
            SetPanel(rankingModePanel, true);
        }

        public void ShowTrainingModePanel()
        {
            SetAllPanels(false);
            SetPanel(trainingModePanel, true);
        }

        public void ShowCharacterBookPanel()
        {
            SetAllPanels(false);
            SetPanel(characterBookPanel, true);
        }

        public void ShowSettingsPanel()
        {
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
            if (panel != null)
                panel.SetActive(active);
        }
    }
}