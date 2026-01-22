using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class SelectedButtonControl : MonoBehaviour
{
    public static List<Character> selectedCharacterList = new List<Character>();
    private const int MAX_SELECTION = 3;
    
    // 선택 상태가 변경될 때 발생하는 이벤트
    public static event Action<int> OnSelectionChanged;
    
    private Color selectedColor = Color.cyan;
    private Color normalColor = Color.white;
    private bool isSelected = false;
    private Image buttonImage;

    [SerializeField]
    private Character characterType;

    // 버튼 클릭음
    [SerializeField]
    private AudioClip buttonClickSound;

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
        // 버튼 클릭 효과음 재생
        if (SFXControl.Instance != null)
        {
            SFXControl.Instance.PlaySFX(buttonClickSound);
        }

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

        // 선택 상태 변경 이벤트 발생
        OnSelectionChanged?.Invoke(selectedCharacterList.Count);

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

        // 선택 상태 변경 이벤트 발생
        OnSelectionChanged?.Invoke(selectedCharacterList.Count);
    }

    // 선택 상태 초기화 (씬 전환 시 등에 사용)
    public static void ClearSelection()
    {
        selectedCharacterList.Clear();
    }

    




}
