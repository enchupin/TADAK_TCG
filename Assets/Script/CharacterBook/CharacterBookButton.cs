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
    [SerializeField] private HandManager handManager; // 매니저 참조


    public void OnButtonClick()
    {
        if (handManager != null) {
            List<int> startdeck = CharacterManager.GetStartDeck(character);
            handManager.AddCardById(startdeck);
        }
        else {
            Debug.LogError("[CharacterBookButton] HandManager reference is missing!");
        }
    }
}
