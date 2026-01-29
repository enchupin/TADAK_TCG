using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 상태를 관리하고 스타트 버튼을 제어하는 매니저
/// 이벤트 기반으로 동작하여 성능이 더 효율적입니다.
/// </summary>
public class CharacterSelectionManagerWithEvent : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "CardTest"; // 이동할 씬 이름
    [SerializeField] private Button startButton; // 스타트 버튼 필드 복구
    private const int REQUIRED_SELECTION = 3;

    private void OnEnable()
    {
        // 이벤트 구독
        SelectedButtonControl.OnSelectionChanged += OnSelectionCountChanged;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        SelectedButtonControl.OnSelectionChanged -= OnSelectionCountChanged;
    }

    private void Start()
    {
        // 초기 상태 설정
        UpdateStartButtonState(SelectedButtonControl.selectedCharacterList.Count);
        
        // 버튼 리스너 연결
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }

    /// <summary>
    /// 게임 시작 버튼 클릭 핸들러
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 선택 개수 검증 (한 번 더 체크)
        if (SelectedButtonControl.selectedCharacterList.Count != REQUIRED_SELECTION)
        {
            Debug.LogWarning($"캐릭터 3명을 선택해야 합니다. (현재: {SelectedButtonControl.selectedCharacterList.Count})");
            return;
        }

        // 씬 전환
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("이동할 씬 이름이 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 선택 개수가 변경될 때 호출되는 콜백
    /// </summary>
    private void OnSelectionCountChanged(int currentCount)
    {
        UpdateStartButtonState(currentCount);
    }

    /// <summary>
    /// 스타트 버튼의 활성화 상태를 업데이트
    /// </summary>
    private void UpdateStartButtonState(int selectionCount)
    {
        if (startButton == null)
        {
            return;
        }

        // 정확히 3개가 선택되었을 때만 활성화
        bool shouldEnable = selectionCount == REQUIRED_SELECTION;
        startButton.interactable = shouldEnable;
    }
}
