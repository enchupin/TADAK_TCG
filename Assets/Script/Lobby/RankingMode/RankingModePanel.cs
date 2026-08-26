using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingModePanel : MonoBehaviour
{
    private const string CharacterProfileResourcePath = "Image/CharacterProfile/";
    private const float LeftColumnX = -65f;
    private const float RightColumnX = 65f;
    private const float StartY = 240f;
    private const float RowSpacing = 120f;

    [Header("캐릭터 버튼 생성 설정")]
    [SerializeField] private Button sourceButton;
    [SerializeField] private RectTransform buttonParent;
    [SerializeField] private RankingModeStarterCardPanel starterCardPanel;
    [SerializeField] private RankingModeSavedDeckPanel savedDeckPanel;

    private readonly List<Button> createdButtons = new List<Button>();
    private readonly Dictionary<Character, Sprite> profileSpriteCache = new Dictionary<Character, Sprite>();

    private void OnEnable()
    {
        CreateButtons();
    }

    public void CreateButtons()
    {
        if (sourceButton == null)
        {
            Debug.LogError("[RankingModePanel] 복사할 버튼이 연결되지 않았습니다");
            return;
        }

        RectTransform parent = ResolveButtonParent();
        if (parent == null)
        {
            Debug.LogError("[RankingModePanel] 버튼을 생성할 부모 RectTransform을 찾을 수 없습니다");
            return;
        }

        ClearCreatedButtons();

        List<CharacterData> characters = CharacterManager.GetAllCharacters();
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("[RankingModePanel] 생성할 캐릭터 데이터가 없습니다");
            return;
        }

        characters.Sort(CompareCharacterOrder);

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData characterData = characters[i];
            if (characterData == null)
            {
                continue;
            }

            Button createdButton = Instantiate(sourceButton, parent, false);
            createdButton.gameObject.SetActive(true);
            createdButton.name = $"RankingModeCharacterButton_{characterData.characterId}";
            SetButtonPosition(createdButton.transform as RectTransform, createdButtons.Count);
            ApplyProfileImage(createdButton, characterData);
            BindCharacterButton(createdButton, characterData);
            createdButtons.Add(createdButton);
        }
    }

    private RectTransform ResolveButtonParent()
    {
        if (buttonParent != null)
        {
            return buttonParent;
        }

        Transform sourceParent = sourceButton != null ? sourceButton.transform.parent : null;
        if (sourceParent is RectTransform sourceParentRect)
        {
            return sourceParentRect;
        }

        return transform as RectTransform;
    }

    private void ClearCreatedButtons()
    {
        for (int i = createdButtons.Count - 1; i >= 0; i--)
        {
            if (createdButtons[i] != null)
            {
                Destroy(createdButtons[i].gameObject);
            }
        }

        createdButtons.Clear();
    }

    private static void SetButtonPosition(RectTransform buttonRect, int buttonIndex)
    {
        if (buttonRect == null)
        {
            return;
        }

        int column = buttonIndex % 2;
        int row = buttonIndex / 2;
        float x = column == 0 ? LeftColumnX : RightColumnX;
        float y = StartY - RowSpacing * row;

        buttonRect.anchoredPosition = new Vector2(x, y);
        buttonRect.localScale = Vector3.one;
    }

    private void ApplyProfileImage(Button button, CharacterData characterData)
    {
        if (button == null || characterData == null)
        {
            return;
        }

        if (!System.Enum.IsDefined(typeof(Character), characterData.characterId))
        {
            Debug.LogWarning($"[RankingModePanel] Character enum에 없는 characterId입니다: {characterData.characterId}");
            return;
        }

        Character character = (Character)characterData.characterId;
        Sprite profileSprite = LoadProfileSprite(character);
        if (profileSprite == null)
        {
            Debug.LogWarning($"[RankingModePanel] 캐릭터 프로필 이미지를 찾을 수 없습니다: {character}");
            return;
        }

        Image buttonImage = button.targetGraphic as Image;
        if (buttonImage == null)
        {
            buttonImage = button.GetComponent<Image>();
        }

        if (buttonImage == null)
        {
            Debug.LogWarning($"[RankingModePanel] 버튼 Image 컴포넌트를 찾을 수 없습니다: {button.name}");
            return;
        }

        buttonImage.sprite = profileSprite;
        buttonImage.preserveAspect = true;
        buttonImage.color = Color.white;
    }

    private void BindCharacterButton(Button button, CharacterData characterData)
    {
        if (button == null || characterData == null)
        {
            return;
        }

        if (!System.Enum.IsDefined(typeof(Character), characterData.characterId))
        {
            return;
        }

        Character character = (Character)characterData.characterId;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ShowCharacterDeckInfo(character));
    }

    private void ShowCharacterDeckInfo(Character character)
    {
        ShowStarterCards(character);
        ShowSavedDecks(character);
    }

    private void ShowStarterCards(Character character)
    {
        RankingModeStarterCardPanel panel = ResolveStarterCardPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 기본카드 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowStarterCards(character);
    }

    private void ShowSavedDecks(Character character)
    {
        RankingModeSavedDeckPanel panel = ResolveSavedDeckPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 저장덱 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowSavedDecks(character);
    }

    private RankingModeStarterCardPanel ResolveStarterCardPanel()
    {
        return starterCardPanel;
    }

    private RankingModeSavedDeckPanel ResolveSavedDeckPanel()
    {
        return savedDeckPanel;
    }

    private Sprite LoadProfileSprite(Character character)
    {
        if (profileSpriteCache.TryGetValue(character, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite profileSprite = LoadSpriteFromResource($"{CharacterProfileResourcePath}{character}");
        if (profileSprite == null && character == Character.Vanessa)
        {
            profileSprite = LoadSpriteFromResource($"{CharacterProfileResourcePath}Venessa");
        }

        profileSpriteCache[character] = profileSprite;
        return profileSprite;
    }

    private static Sprite LoadSpriteFromResource(string resourcePath)
    {
        Sprite profileSprite = Resources.Load<Sprite>(resourcePath);
        if (profileSprite != null)
        {
            return profileSprite;
        }

        Sprite[] profileSprites = Resources.LoadAll<Sprite>(resourcePath);
        return profileSprites != null && profileSprites.Length > 0 ? profileSprites[0] : null;
    }

    private static int CompareCharacterOrder(CharacterData left, CharacterData right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.characterId.CompareTo(right.characterId);
    }
}
