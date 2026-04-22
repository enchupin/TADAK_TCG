using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BattleStandingUI : MonoBehaviour
{
    [Header("캐릭터 스탠딩 이미지")]
    [SerializeField] private Image firstStandingImage;
    [SerializeField] private Image secondStandingImage;
    [SerializeField] private Image thirdStandingImage;

    private readonly Dictionary<Character, Sprite> spriteCache = new();
    private string currentSelectionSignature;

    private void OnEnable()
    {
        currentSelectionSignature = null;
        RefreshIfNeeded();
    }

    private void Start()
    {
        RefreshIfNeeded();
    }

    private void Update()
    {
        RefreshIfNeeded();
    }

    public void Refresh()
    {
        List<Character> selectedCharacters = SelectedButtonControl.selectedCharacterList;

        SetStandingImage(firstStandingImage, selectedCharacters, 0);
        SetStandingImage(secondStandingImage, selectedCharacters, 1);
        SetStandingImage(thirdStandingImage, selectedCharacters, 2);
        currentSelectionSignature = BuildSelectionSignature(selectedCharacters);
    }

    private void RefreshIfNeeded()
    {
        List<Character> selectedCharacters = SelectedButtonControl.selectedCharacterList;
        string nextSelectionSignature = BuildSelectionSignature(selectedCharacters);
        if (currentSelectionSignature == nextSelectionSignature)
        {
            return;
        }

        Refresh();
    }

    private void SetStandingImage(Image targetImage, List<Character> selectedCharacters, int index)
    {
        if (targetImage == null)
        {
            return;
        }

        if (selectedCharacters == null || index < 0 || index >= selectedCharacters.Count)
        {
            targetImage.sprite = null;
            targetImage.enabled = false;
            return;
        }

        Character character = selectedCharacters[index];
        Sprite standingSprite = LoadStandingSprite(character);
        targetImage.sprite = standingSprite;
        targetImage.enabled = standingSprite != null;
        targetImage.preserveAspect = true;
    }

    private Sprite LoadStandingSprite(Character character)
    {
        if (spriteCache.TryGetValue(character, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        string resourcePath = BuildStandingResourcePath(character);
        Sprite standingSprite = Resources.Load<Sprite>(resourcePath);
        if (standingSprite == null)
        {
            Debug.LogWarning($"캐릭터 스탠딩 이미지를 찾을 수 없습니다: {resourcePath}");
            return null;
        }

        spriteCache[character] = standingSprite;
        return standingSprite;
    }

    private static string BuildStandingResourcePath(Character character)
    {
        return $"Image/BattleStanding/BattleStanding_{character}";
    }

    private static string BuildSelectionSignature(List<Character> selectedCharacters)
    {
        if (selectedCharacters == null || selectedCharacters.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        int count = Mathf.Min(3, selectedCharacters.Count);
        for (int i = 0; i < count; i++)
        {
            builder.Append((int)selectedCharacters[i]);
            builder.Append('|');
        }

        return builder.ToString();
    }
}
