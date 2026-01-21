using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TADAK.Lobby
{
    /// <summary>
    /// 캐릭터 선택 관리자 - 최대 3개까지 선택 가능
    /// </summary>
    public class CharacterSelectionManager : MonoBehaviour
    {
        public static CharacterSelectionManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private int maxSelectionCount = 3;
        
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform characterButtonContainer;
        [SerializeField] private Text selectionCountText;
        [SerializeField] private Button confirmButton;
        
        private List<CharacterClassButton> allButtons = new List<CharacterClassButton>();
        private List<Character> selectedCharacters = new List<Character>();
        
        public int MaxSelectionCount => maxSelectionCount;
        public int CurrentSelectionCount => selectedCharacters.Count;
        public IReadOnlyList<Character> SelectedCharacters => selectedCharacters.AsReadOnly();
        
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeButtons();
            UpdateUI();
            
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            }
        }
        
        private void InitializeButtons()
        {
            // 자식 오브젝트에서 모든 CharacterClassButton 찾기
            if (characterButtonContainer != null)
            {
                allButtons.AddRange(characterButtonContainer.GetComponentsInChildren<CharacterClassButton>(true));
            }
            else
            {
                allButtons.AddRange(GetComponentsInChildren<CharacterClassButton>(true));
            }
            
            Debug.Log($"[CharacterSelectionManager] {allButtons.Count}개의 캐릭터 버튼을 찾았습니다.");
        }
        
        /// <summary>
        /// 캐릭터 버튼 클릭 처리
        /// </summary>
        public void OnCharacterButtonClicked(CharacterClassButton button)
        {
            if (button == null) return;
            
            if (button.IsSelected)
            {
                // 이미 선택된 버튼 -> 선택 해제
                DeselectCharacter(button);
            }
            else
            {
                // 선택되지 않은 버튼 -> 선택 시도
                SelectCharacter(button);
            }
        }
        
        /// <summary>
        /// 캐릭터 선택
        /// </summary>
        private void SelectCharacter(CharacterClassButton button)
        {
            // 최대 선택 개수 확인
            if (selectedCharacters.Count >= maxSelectionCount)
            {
                Debug.LogWarning($"[CharacterSelectionManager] 최대 {maxSelectionCount}개까지만 선택할 수 있습니다.");
                // TODO: 사용자에게 알림 UI 표시
                return;
            }
            
            // 선택 추가
            selectedCharacters.Add(button.Character);
            button.SetSelected(true);
            
            Debug.Log($"[CharacterSelectionManager] {button.Character} 선택됨 ({selectedCharacters.Count}/{maxSelectionCount})");
            
            UpdateUI();
        }
        
        /// <summary>
        /// 캐릭터 선택 해제
        /// </summary>
        private void DeselectCharacter(CharacterClassButton button)
        {
            selectedCharacters.Remove(button.Character);
            button.SetSelected(false);
            
            Debug.Log($"[CharacterSelectionManager] {button.Character} 선택 해제됨 ({selectedCharacters.Count}/{maxSelectionCount})");
            
            UpdateUI();
        }
        
        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            // 선택 개수 텍스트 업데이트
            if (selectionCountText != null)
            {
                selectionCountText.text = $"선택된 캐릭터: {selectedCharacters.Count}/{maxSelectionCount}";
            }
            
            // 확인 버튼 활성화/비활성화
            if (confirmButton != null)
            {
                confirmButton.interactable = selectedCharacters.Count > 0;
            }
        }
        
        /// <summary>
        /// 확인 버튼 클릭 처리
        /// </summary>
        private void OnConfirmButtonClicked()
        {
            if (selectedCharacters.Count == 0)
            {
                Debug.LogWarning("[CharacterSelectionManager] 선택된 캐릭터가 없습니다.");
                return;
            }
            
            // 선택된 캐릭터 데이터 저장
            SelectedCharactersData.Instance.SetSelectedCharacters(selectedCharacters);
            
            Debug.Log($"[CharacterSelectionManager] {selectedCharacters.Count}개의 캐릭터가 선택되어 저장되었습니다.");
            
            // TODO: 다음 씬으로 이동
            // UnityEngine.SceneManagement.SceneManager.LoadScene("NextSceneName");
        }
        
        /// <summary>
        /// 모든 선택 초기화
        /// </summary>
        public void ClearSelection()
        {
            foreach (var button in allButtons)
            {
                button.SetSelected(false);
            }
            
            selectedCharacters.Clear();
            UpdateUI();
            
            Debug.Log("[CharacterSelectionManager] 모든 선택이 초기화되었습니다.");
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
            }
        }
    }
}
