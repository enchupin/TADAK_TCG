using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterLocalizedButtonLabel : MonoBehaviour
{
    [SerializeField] private int characterId;

    private TMP_Text tmpText;
    private Text legacyText;

    public void Bind(int localizedCharacterId)
    {
        characterId = localizedCharacterId;
        CacheTextReferences();
        RefreshLabel();
    }

    private void OnEnable()
    {
        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        CacheTextReferences();
        RefreshLabel();
    }

    private void OnDisable()
    {
        LocalizationManager.LanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged()
    {
        RefreshLabel();
    }

    private void CacheTextReferences()
    {
        if (tmpText == null)
        {
            tmpText = GetComponentInChildren<TMP_Text>(true);
        }

        if (legacyText == null)
        {
            legacyText = GetComponentInChildren<Text>(true);
        }
    }

    private void RefreshLabel()
    {
        if (characterId <= 0)
        {
            return;
        }

        CharacterData character = CharacterManager.GetCharacter(characterId);
        string localizedCharacterName = character != null ? character.characterName : string.Empty;

        if (tmpText != null)
        {
            tmpText.text = localizedCharacterName;
            return;
        }

        if (legacyText != null)
        {
            legacyText.text = localizedCharacterName;
        }
    }
}
