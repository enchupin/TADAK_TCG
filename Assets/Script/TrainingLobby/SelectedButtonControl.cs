using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SelectedButtonControl : MonoBehaviour
{
    public static List<Character> selectedCharacterList = new List<Character>();
    private const int MAX_SELECTION = 3;
    
    private Color selectedColor = Color.cyan;
    private Color normalColor = Color.white;
    private bool isSelected = false;
    
    private Image buttonImage;
    [SerializeField]
    private Character characterType;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("Button에 Image 컴포넌트가 없습니다!");
        }
        ClearSelection();
    }


    public void OnCharacterSelectButtonClicked()
    {
        if (isSelected)
        {
            // 이미 선택된 버튼을 다시 클릭 -> 선택 해제
            DeselectCharacter();
        }
        else
        {
            // 선택되지 않은 버튼을 클릭 -> 선택
            SelectCharacter();
        }
    }

    private void SelectCharacter()
    {
        // 최대 선택 개수 체크
        if (selectedCharacterList.Count >= MAX_SELECTION)
        {
            return;
        }

        // 리스트에 추가
        selectedCharacterList.Add(characterType);
        
        // 선택 상태로 변경
        isSelected = true;
        
        // 버튼 색상을 하늘색으로 변경
        if (buttonImage != null)
        {
            buttonImage.color = selectedColor;
        }

    }

    private void DeselectCharacter()
    {
        // 리스트에서 제거
        selectedCharacterList.Remove(characterType);
        
        // 선택 해제 상태로 변경
        isSelected = false;
        
        // 버튼 색상을 원래대로 복원
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }

    // 선택 상태 초기화 (씬 전환 시 등에 사용)
    public static void ClearSelection()
    {
        selectedCharacterList.Clear();
    }

    




}
