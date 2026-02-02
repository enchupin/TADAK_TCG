using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// DeckList UI 컨트롤러
/// 특정 캐릭터의 저장된 덱 목록을 표시
/// </summary>
public class DeckListController : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private Transform deckButtonContainer; // 덱 버튼들이 들어갈 컨테이너
    [SerializeField] private GameObject deckListPrefab; // 덱 리스트 프리팹

    private Character assignedCharacter;
    private SaveDeck saveDeck;


    /// <summary>
    /// 개별 덱 버튼 생성
    /// </summary>
    private void CreateDeckButton(DeckData deckData)
    {
        GameObject deckButton = Instantiate(deckListPrefab, deckButtonContainer);
        
        // 버튼 텍스트 설정 (Text 컴포넌트가 있다면)
        Text buttonText = deckButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = $"{deckData.deckName}\n({deckData.cardIds.Count}장)";
        }

        // 버튼 클릭 이벤트 설정 (Button 컴포넌트가 있다면)
        Button button = deckButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnDeckButtonClick(deckData));
        }
    }

    /// <summary>
    /// 덱 버튼 클릭 시 호출
    /// </summary>
    private void OnDeckButtonClick(DeckData deckData)
    {
        Debug.Log($"[DeckListController] 덱 선택: {deckData.deckName}");
        // TODO: 덱 선택 로직 구현 (랭킹 모드에서 사용할 덱 선택)
    }

}
