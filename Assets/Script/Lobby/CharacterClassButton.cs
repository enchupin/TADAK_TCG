using UnityEngine;
using UnityEngine.UI;

namespace TADAK.Lobby
{
    /// <summary>
    /// 캐릭터 직업 선택 버튼 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class CharacterClassButton : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Character character;
        
        [Header("Visual")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.green;
        
        private Button button;
        private bool isSelected = false;
        
        public Character Character => character;
        public bool IsSelected => isSelected;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }
            
            button.onClick.AddListener(OnButtonClick);
            UpdateVisual();
        }
        
        private void OnButtonClick()
        {
            CharacterSelectionManager.Instance?.OnCharacterButtonClicked(this);
        }
        
        /// <summary>
        /// 선택 상태 설정
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisual();
        }
        
        /// <summary>
        /// 버튼 시각적 상태 업데이트
        /// </summary>
        private void UpdateVisual()
        {
            if (buttonImage != null)
            {
                buttonImage.color = isSelected ? selectedColor : normalColor;
            }
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}
