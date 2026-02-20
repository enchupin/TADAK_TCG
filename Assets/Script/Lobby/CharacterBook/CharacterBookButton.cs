using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 도감에서 캐릭터 선택 버튼에 부착되는 스크립트
/// 클릭 시 해당 캐릭터의 카드를 보여주도록 매니저에게 요청
/// </summary>
public class CharacterBookButton : MonoBehaviour
{
    [SerializeField] private Character character; // 이 버튼이 담당하는 캐릭터
    [SerializeField] private CardContainerManager cardManager; // 매니저 참조


    public void OnButtonClick()
    {
        if (cardManager != null) {
            List<CardData> characterCards = CardManager.GetCardsByCharacter(character);
            List<Card> cardObjects = new List<Card>();
             foreach (CardData data in characterCards) {
                if (data != null) {
                    cardObjects.Add(data.ToCard());
                }
            }
            cardManager.ClearHand();
            cardManager.AddCardWithoutInputController(cardObjects);
        }
        else {
            Debug.LogError("[CharacterBookButton] HandManager reference is missing!");
        }
    }
}
