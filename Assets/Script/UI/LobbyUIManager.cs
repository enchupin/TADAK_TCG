using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UI
{
    /// <summary>
    /// 로비 씬의 UI 및 버튼 동작을 관리하는 메인 컨트롤러
    /// </summary>
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("UI 버튼 참조")]
        [SerializeField] private Button trainingButton;      // 훈련입장
        [SerializeField] private Button weeklyTournamentButton; // 주간대회
        [SerializeField] private Button characterDexButton;  // 캐릭터도감
        [SerializeField] private Button recordButton;        // 기록
        [SerializeField] private Button settingsButton;      // 설정
        [SerializeField] private Button quitButton;          // 종료

        private void Start()
        {
            InitializeButtons();
        }

        private void InitializeButtons()
        {
            // 각 버튼에 클릭 이벤트 연결
            if (trainingButton != null)
            {
                trainingButton.onClick.AddListener(OnTrainingButtonClicked);
            }

            if (weeklyTournamentButton != null)
            {
                weeklyTournamentButton.onClick.AddListener(OnWeeklyTournamentButtonClicked);
            }

            if (characterDexButton != null)
            {
                characterDexButton.onClick.AddListener(OnCharacterDexButtonClicked);
            }

            if (recordButton != null)
            {
                recordButton.onClick.AddListener(OnRecordButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }
        }

        #region 버튼 클릭 이벤트 핸들러

        private void OnTrainingButtonClicked()
        {
            Debug.Log("훈련입장 버튼 클릭됨");
            // TODO: 훈련 씬으로 이동
            // SceneManager.LoadScene("TrainingScene");
        }

        private void OnWeeklyTournamentButtonClicked()
        {
            Debug.Log("주간대회 버튼 클릭됨");
            // TODO: 주간대회 씬으로 이동
            // SceneManager.LoadScene("WeeklyTournamentScene");
        }

        private void OnCharacterDexButtonClicked()
        {
            Debug.Log("캐릭터도감 버튼 클릭됨");
            // TODO: 캐릭터도감 씬으로 이동
            // SceneManager.LoadScene("CharacterDexScene");
        }

        private void OnRecordButtonClicked()
        {
            Debug.Log("기록 버튼 클릭됨");
            // TODO: 기록 UI 표시 또는 씬 이동
            // SceneManager.LoadScene("RecordScene");
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("설정 버튼 클릭됨");
            // TODO: 설정 UI 표시
            // SettingsPanel.Show();
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("종료 버튼 클릭됨");
            
            // 에디터에서는 플레이모드 종료, 빌드에서는 애플리케이션 종료
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        private void OnDestroy()
        {
            // 이벤트 리스너 정리
            if (trainingButton != null)
                trainingButton.onClick.RemoveListener(OnTrainingButtonClicked);
            
            if (weeklyTournamentButton != null)
                weeklyTournamentButton.onClick.RemoveListener(OnWeeklyTournamentButtonClicked);
            
            if (characterDexButton != null)
                characterDexButton.onClick.RemoveListener(OnCharacterDexButtonClicked);
            
            if (recordButton != null)
                recordButton.onClick.RemoveListener(OnRecordButtonClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitButtonClicked);
        }
    }
}
