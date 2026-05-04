using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 도감에서 캐릭터 선택 버튼에 부착되는 스크립트
/// 클릭 시 해당 캐릭터의 카드를 보여주도록 매니저에게 요청
/// </summary>
public class CharacterBookButton : MonoBehaviour
{
    private const string ButtonClickSoundPath = "Sound/click5";
    private const float ButtonClickSoundVolumeScale = 1.25f;

    private Character character; // 이 버튼이 담당하는 캐릭터
    private CardContainerManager cardManager; // 매니저 참조
    private static AudioClip buttonClickSound; // 버튼 클릭음
    private static bool isButtonClickSoundLoadTried;

    private void Awake()
    {
        LoadButtonClickSound();
    }


    public void Initialize(Character targetCharacter, CardContainerManager targetCardManager)
    {
        character = targetCharacter;
        cardManager = targetCardManager;
    }

    private static void LoadButtonClickSound()
    {
        if (isButtonClickSoundLoadTried) {
            return;
        }

        isButtonClickSoundLoadTried = true;
        buttonClickSound = Resources.Load<AudioClip>(ButtonClickSoundPath);
        if (buttonClickSound == null) {
            Debug.LogError($"[CharacterBookButton] 버튼 클릭음을 불러오지 못했습니다: {ButtonClickSoundPath}");
        }
    }


    public void OnButtonClick()
    {
        if (SFXControl.Instance != null) {
            SFXControl.Instance.PlaySFX(buttonClickSound, ButtonClickSoundVolumeScale);
        }
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
