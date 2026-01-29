using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

public class SelectedButtonControl : MonoBehaviour
{
    // 정적 변수
    public static List<Character> selectedCharacterList = new List<Character>();
    private const int MAX_SELECTION = 3;
    
    // 선택 상태가 변경될 때 발생하는 이벤트
    public static event Action<int> OnSelectionChanged;


    [SerializeField]
    private Character characterType;

    [SerializeField]
    private AudioClip buttonClickSound; // 로드된 버튼 클릭음

    private bool isSelected = false;





    // 임시 변수
    private Color selectedColor = Color.cyan;
    private Color normalColor = Color.white;
    private Image characterImage;




    private void Awake()
    {

        // 임시코드
        characterImage = GetComponent<Image>();
        if (characterImage == null)
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


    /// <summary>
    /// 캐릭터 선택
    /// </summary>
    private void SelectCharacter()
    {
        // 최대 선택 개수 체크
        if (selectedCharacterList.Count >= MAX_SELECTION)
        {
            return;
        }

        // 버튼 클릭 효과음 재생
        if (SFXControl.Instance != null) {
            SFXControl.Instance.PlaySFX(buttonClickSound);
        }

        // 리스트에 추가
        selectedCharacterList.Add(characterType);
        
        // 선택 상태로 변경
        isSelected = true;
        
        // 버튼 색상을 하늘색으로 변경
        if (characterImage != null)
        {
            characterImage.color = selectedColor;
        }

        // 선택 상태 변경 이벤트 발생
        OnSelectionChanged?.Invoke(selectedCharacterList.Count);

    }


    /// <summary>
    /// 캐릭터 선택 해제
    /// </summary>
    private void DeselectCharacter()
    {
        // 리스트에서 제거
        selectedCharacterList.Remove(characterType);
        
        // 선택 해제 상태로 변경
        isSelected = false;
        
        // 버튼 색상을 원래대로 복원
        if (characterImage != null)
        {
            characterImage.color = normalColor;
        }

        // 선택 상태 변경 이벤트 발생
        OnSelectionChanged?.Invoke(selectedCharacterList.Count);
    }

    // 선택 상태 초기화 (씬 전환 시 등에 사용)
    private static void ClearSelection()
    {
        selectedCharacterList.Clear();
    }

}
