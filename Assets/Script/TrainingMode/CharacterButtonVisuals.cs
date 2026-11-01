using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 버튼 공통 표시: 현재 언어 이름 · 역할 아이콘 · 스탠딩 이미지
/// 훈련 모드 · 보스 모드 버튼이 함께 사용
/// </summary>
public static class CharacterButtonVisuals
{
    public static void Apply(GameObject buttonObject, CharacterData characterData, bool applyStandingImage = true)
    {
        if (buttonObject == null || characterData == null)
        {
            return;
        }

        BindLabel(buttonObject, characterData.characterId);
        SetButtonText(buttonObject, characterData.characterName);
        BindRoleIcon(buttonObject, characterData.characterId);

        if (applyStandingImage && System.Enum.IsDefined(typeof(Character), characterData.characterId))
        {
            CharacterStandingButtonImageUtility.Apply(buttonObject, (Character)characterData.characterId);
        }
    }

    private static void BindLabel(GameObject buttonObject, int characterId)
    {
        CharacterLocalizedButtonLabel localizedLabel = buttonObject.GetComponent<CharacterLocalizedButtonLabel>();
        if (localizedLabel == null)
        {
            localizedLabel = buttonObject.AddComponent<CharacterLocalizedButtonLabel>();
        }

        localizedLabel.Bind(characterId);
    }

    // 좌측 상단 역할 아이콘 (프리팹에 CharacterRoleIcon이 없으면 생략)
    private static void BindRoleIcon(GameObject buttonObject, int characterId)
    {
        CharacterRoleIcon roleIcon = buttonObject.GetComponentInChildren<CharacterRoleIcon>(true);
        if (roleIcon != null)
        {
            roleIcon.Bind(characterId);
        }
    }

    private static void SetButtonText(GameObject buttonObject, string characterName)
    {
        TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = characterName;
            return;
        }

        Text legacyText = buttonObject.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = characterName;
        }
    }
}
