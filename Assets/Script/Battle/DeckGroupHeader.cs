using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전체 덱 화면의 캐릭터 묶음 머리줄 (프로필 · 이름 · 구분선은 프리팹에서 배치)
/// </summary>
public class DeckGroupHeader : MonoBehaviour
{
    private const string ProfileResourceFolder = "Image/Character/CharacterProfile/";

    [SerializeField] private Image profileImage;
    [SerializeField] private TMP_Text nameText;

    // 파티 캐릭터 묶음: 프로필 + 현재 언어 이름
    public void BindCharacter(Character character)
    {
        Sprite profile = Resources.Load<Sprite>(ProfileResourceFolder + character);
        if (profile == null)
        {
            Debug.LogWarning($"[DeckGroupHeader] 캐릭터 프로필 이미지를 찾을 수 없습니다: {ProfileResourceFolder}{character}");
        }

        SetProfile(profile);
        SetName(GameText.CharacterName((int)character, character.ToString()));
    }

    // 파티 캐릭터에 속하지 않는 카드 묶음: 프로필 없이 이름만
    public void BindOther()
    {
        SetProfile(null);
        SetName(GameText.Ui("ui.deck.otherCards"));
    }

    private void SetProfile(Sprite sprite)
    {
        if (profileImage == null)
        {
            return;
        }

        profileImage.sprite = sprite;
        profileImage.enabled = sprite != null;
    }

    private void SetName(string text)
    {
        if (nameText != null)
        {
            nameText.text = text ?? string.Empty;
        }
    }
}
