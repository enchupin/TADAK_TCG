using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 상태를 관리하고 스타트 버튼을 제어하는 매니저
/// 이벤트 기반으로 동작하여 성능이 더 효율적입니다.
/// </summary>
public class CharacterSelectionManagerWithEvent : MonoBehaviour
{
    [SerializeField] private Button startButton;
    private const int REQUIRED_SELECTION = 3;

    private void OnEnable()
    {
        // 이벤트 구독
        SelectedButtonControl.OnSelectionChanged += OnSelectionCountChanged;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        SelectedButtonControl.OnSelectionChanged -= OnSelectionCountChanged;
    }

    private void Start()
    {
        // 초기 상태 설정
        UpdateStartButtonState(SelectedButtonControl.selectedCharacterList.Count);
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
            Debug.LogError("Start Button이 할당되지 않았습니다!");
            return;
        }

        // 정확히 3개가 선택되었을 때만 활성화
        bool shouldEnable = selectionCount == REQUIRED_SELECTION;
        startButton.interactable = shouldEnable;
    }
}
