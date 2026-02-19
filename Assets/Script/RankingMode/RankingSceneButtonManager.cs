using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingSceneButtonManager : MonoBehaviour
{
    [Header("버튼 리스트 (순서대로 등록해주세요)")]
    [SerializeField] private List<Button> characterButtons;

    [Header("레이아웃 설정")]
    [SerializeField] private float startXPosition = 0f; // 시작 X 좌표
    [SerializeField] private float spawnSpacing = 20f; // 간격
    [SerializeField] private float expandShiftAmount = 160f; // 버튼 클릭 시 뒤 버튼들이 이동하는 거리

    private List<CharacterSelectButton_RankingScene> currentExpandedBtnList = new(); // 현재 활성화된 선택창 오브젝트

    private void Start()
    {
        // UpdateLayout();
    }

    /// <summary>
    /// 버튼 클릭 시 호출되는 함수
    /// </summary>
    public void OnCharacterButtonClicked(CharacterSelectButton_RankingScene clickedButton)
    {
        // 이미 열려있는 버튼을 다시 누르면 닫기
        if (currentExpandedBtnList.Contains(clickedButton))
        {
            if (clickedButton != null) clickedButton.CloseCurrentExpanded();
            currentExpandedBtnList.Remove(clickedButton);

            // 뒤쪽 버튼들을 왼쪽으로 이동 (닫기)
            ShiftButtonsAfter(clickedButton, -expandShiftAmount);
            return;
        }

        // 덱 리스트 버튼 열기
        if (clickedButton != null)
        {
            clickedButton.OpenExpandedMenu();
            currentExpandedBtnList.Add(clickedButton);

            // 뒤쪽 버튼들을 오른쪽으로 이동 (열기)
            ShiftButtonsAfter(clickedButton, expandShiftAmount);
        }
    }

    /// <summary>
    /// 클릭한 버튼보다 뒤에 있는 버튼들을 X축으로 shiftAmount만큼 이동
    /// </summary>
    private void ShiftButtonsAfter(CharacterSelectButton_RankingScene clickedButton, float shiftAmount)
    {
        // 클릭한 버튼의 인덱스를 찾음
        int clickedIndex = -1;
        for (int i = 0; i < characterButtons.Count; i++)
        {
            if (characterButtons[i] == null) continue;
            var comp = characterButtons[i].GetComponent<CharacterSelectButton_RankingScene>();
            if (comp == clickedButton)
            {
                clickedIndex = i;
                break;
            }
        }

        if (clickedIndex == -1) return; // 못 찾으면 종료

        // 클릭한 버튼보다 뒤에 있는 버튼들만 이동
        for (int i = clickedIndex + 1; i < characterButtons.Count; i++)
        {
            if (characterButtons[i] == null) continue;

            RectTransform rt = characterButtons[i].GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 pos = rt.anchoredPosition;
                pos.x += shiftAmount;
                rt.anchoredPosition = pos;
            }
        }
    }
}
