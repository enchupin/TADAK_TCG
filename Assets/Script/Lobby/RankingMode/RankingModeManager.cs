using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RankingModeManager : MonoBehaviour
{
    private const string SpawnedRootName = "RankingSavedDeckButtonRoot";
    private const int RequiredSelectionCount = 3;
    private const string DefaultDeckName = "기본 덱";
    private const float DefaultContentWidth = 800f;
    private const float DefaultContentHeight = 370f;
    private const float ContentTrailingPadding = 180f;




    private sealed class SavedDeckButtonBinding
    {
        public Button button;
        public Character character;
        public CharacterDeckSave deck;
        public string baseLabel;
    }

    [Header("저장덱 버튼 생성 설정")]
    [SerializeField] private Button deckButtonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private SceneChanger sceneChanger;
    [SerializeField] private string rankingBattleSceneName = "TrainingScene";

    [Header("저장덱 버튼 배치 설정")]
    [SerializeField] private Vector2 startAnchoredPosition = Vector2.zero;
    private const float buttonSpacing = 220f;

    private readonly List<SavedDeckButtonBinding> deckButtonBindings = new List<SavedDeckButtonBinding>();
    private readonly Dictionary<Character, RankingModeSelectedDeck> selectedDecksByCharacter = new Dictionary<Character, RankingModeSelectedDeck>();
    private Transform spawnedRoot;

    private void OnEnable()
    {
        OpenRankingMode();
    }

    public void OpenRankingMode()
    {
        selectedDecksByCharacter.Clear();
        SelectedButtonControl.ClearSelection();
        CreateDeckButtons();
    }

    public void CreateDeckButtons()
    {
        if (deckButtonPrefab == null)
        {
            Debug.LogError("[RankingModeManager] 저장덱 버튼 프리팹이 연결되지 않았습니다");
            return;
        }

        if (buttonParent == null)
        {
            ClearSpawnedButtons();
            Debug.LogError("[RankingModeManager] 저장덱 버튼을 생성할 Content가 연결되지 않았습니다");
            return;
        }

        ClearSpawnedButtons();

        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        if (profile == null || profile.characters == null)
        {
            Debug.LogWarning("[RankingModeManager] 저장덱을 불러올 프로필이 없습니다");
            return;
        }

        Transform parent = ResolveSpawnedRoot();
        int createdCount = 0;
        List<CharacterData> characters = CharacterManager.GetAllCharacters();
        characters.Sort(CompareCharacterOrder);

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData characterData = characters[i];
            if (characterData == null)
            {
                continue;
            }

            CharacterDeckLibrarySave library = profile.FindLibrary(characterData.characterId);
            List<CharacterDeckSave> displayDecks = CollectDisplayDecks(characterData.characterId, library);
            for (int deckIndex = 0; deckIndex < displayDecks.Count; deckIndex++)
            {
                CharacterDeckSave deck = displayDecks[deckIndex];

                Button button = Instantiate(deckButtonPrefab, parent, false);
                button.gameObject.SetActive(true);
                button.name = $"RankingDeckButton_{characterData.characterId}_{createdCount}";
                SetButtonPosition(button.transform as RectTransform, createdCount);

                Character character = CharacterManager.GetCharacterEnumById(characterData.characterId);
                string label = CreateDeckLabel(characterData.characterId, deck);
                SavedDeckButtonBinding binding = new SavedDeckButtonBinding
                {
                    button = button,
                    character = character,
                    deck = deck,
                    baseLabel = label
                };

                deckButtonBindings.Add(binding);
                SetButtonText(button.gameObject, label);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ToggleDeckSelection(binding));
                createdCount++;
            }
        }

        UpdateContentSize(createdCount);
        RefreshSelectionView();
    }

    private static List<CharacterDeckSave> CollectDisplayDecks(int characterId, CharacterDeckLibrarySave library)
    {
        List<CharacterDeckSave> displayDecks = new List<CharacterDeckSave>();
        if (library?.decks != null)
        {
            for (int deckIndex = 0; deckIndex < library.decks.Count; deckIndex++)
            {
                CharacterDeckSave deck = library.decks[deckIndex];
                if (ShouldShowDeck(characterId, deck))
                {
                    displayDecks.Add(deck);
                }
            }
        }

        if (displayDecks.Count == 0)
        {
            displayDecks.Add(CreateDefaultDeck(characterId));
        }

        return displayDecks;
    }

    private static CharacterDeckSave CreateDefaultDeck(int characterId)
    {
        return new CharacterDeckSave
        {
            deckId = $"default-{characterId}",
            name = DefaultDeckName,
            cardIds = CharacterManager.GetStarterCardIds(characterId)
        };
    }

    public void ClearSpawnedButtons()
    {
        Transform root = buttonParent != null ? buttonParent.Find(SpawnedRootName) : null;
        if (root != null)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            spawnedRoot = root;
        }

        if (spawnedRoot != null && spawnedRoot.parent != buttonParent)
        {
            Destroy(spawnedRoot.gameObject);
            spawnedRoot = null;
        }

        deckButtonBindings.Clear();
    }

    private Transform ResolveSpawnedRoot()
    {
        if (spawnedRoot != null)
        {
            return spawnedRoot;
        }

        spawnedRoot = buttonParent.Find(SpawnedRootName);
        if (spawnedRoot != null)
        {
            return spawnedRoot;
        }

        GameObject rootObject = new GameObject(SpawnedRootName, typeof(RectTransform));
        spawnedRoot = rootObject.transform;
        spawnedRoot.SetParent(buttonParent, false);

        RectTransform rootRect = spawnedRoot as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = new Vector2(0f, 0.5f);
            rootRect.anchorMax = new Vector2(0f, 0.5f);
            rootRect.pivot = new Vector2(0f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.localScale = Vector3.one;
        }

        return spawnedRoot;
    }

    private void SetButtonPosition(RectTransform buttonRect, int buttonIndex)
    {
        if (buttonRect == null)
        {
            return;
        }

        Vector2 position = startAnchoredPosition;
        position.x += buttonSpacing * buttonIndex;

        buttonRect.anchoredPosition = position;
        buttonRect.localScale = Vector3.one;
    }

    private void ToggleDeckSelection(SavedDeckButtonBinding binding)
    {
        if (binding == null || binding.deck == null)
        {
            return;
        }

        if (selectedDecksByCharacter.TryGetValue(binding.character, out RankingModeSelectedDeck selectedDeck)
            && selectedDeck.deckId == binding.deck.deckId)
        {
            selectedDecksByCharacter.Remove(binding.character);
            SyncSelectedCharacters();
            RefreshSelectionView();
            return;
        }

        if (!selectedDecksByCharacter.ContainsKey(binding.character)
            && selectedDecksByCharacter.Count >= RequiredSelectionCount)
        {
            Debug.LogWarning("[RankingModeManager] 랭킹모드는 서로 다른 캐릭터의 저장덱 3개까지만 선택할 수 있습니다");
            return;
        }

        selectedDecksByCharacter[binding.character] = new RankingModeSelectedDeck(binding.character, binding.deck);
        SyncSelectedCharacters();
        RefreshSelectionView();
    }

    private void SyncSelectedCharacters()
    {
        SelectedButtonControl.selectedCharacterList.Clear();
        foreach (RankingModeSelectedDeck selectedDeck in CreateOrderedSelectedDecks())
        {
            SelectedButtonControl.selectedCharacterList.Add(selectedDeck.character);
        }
    }

    private void RefreshSelectionView()
    {
        foreach (SavedDeckButtonBinding binding in deckButtonBindings)
        {
            if (binding?.button == null)
            {
                continue;
            }

            bool isSelected = selectedDecksByCharacter.TryGetValue(binding.character, out RankingModeSelectedDeck selectedDeck)
                && selectedDeck.deckId == binding.deck.deckId;
            SetButtonText(binding.button.gameObject, isSelected ? $"{binding.baseLabel}\n[선택됨]" : binding.baseLabel);

            Image image = binding.button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected ? Color.cyan : Color.white;
            }
        }

    }

    public void StartRankingMode()
    {
        List<RankingModeSelectedDeck> selectedDecks = CreateOrderedSelectedDecks();
        if (!RankingModeSession.TryStartFromSelectedDecks(selectedDecks))
        {
            return;
        }

        TrainingBattleManager.buildingDeck = null;
        PlayerData.Reset();
        TrainingRunState.ResetRun();
        InfiniteMode.SetMode(false);
        SceneManager.LoadScene(ResolveRankingBattleSceneName());
    }

    private string ResolveRankingBattleSceneName()
    {
        if (!string.IsNullOrWhiteSpace(rankingBattleSceneName))
        {
            return rankingBattleSceneName;
        }

        if (sceneChanger == null)
        {
            sceneChanger = FindAnyObjectByType<SceneChanger>();
        }

        return "TrainingScene";
    }

    private List<RankingModeSelectedDeck> CreateOrderedSelectedDecks()
    {
        List<RankingModeSelectedDeck> orderedDecks = new List<RankingModeSelectedDeck>();
        HashSet<Character> addedCharacters = new HashSet<Character>();
        foreach (SavedDeckButtonBinding binding in deckButtonBindings)
        {
            if (binding == null || !addedCharacters.Add(binding.character))
            {
                continue;
            }

            if (selectedDecksByCharacter.TryGetValue(binding.character, out RankingModeSelectedDeck selectedDeck))
            {
                orderedDecks.Add(selectedDeck);
            }
        }

        return orderedDecks;
    }

    private void UpdateContentSize(int buttonCount)
    {
        RectTransform contentRect = buttonParent as RectTransform;
        if (contentRect == null || buttonCount <= 0)
        {
            return;
        }

        RectTransform prefabRect = deckButtonPrefab.transform as RectTransform;
        float buttonWidth = prefabRect != null ? prefabRect.rect.width : 160f;
        float buttonHeight = prefabRect != null ? prefabRect.rect.height : 30f;
        float requiredWidth = Mathf.Max(
            DefaultContentWidth,
            startAnchoredPosition.x + buttonSpacing * (buttonCount - 1) + buttonWidth + ContentTrailingPadding);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(contentRect.rect.height, DefaultContentHeight));
        return;
    }

    private static bool ShouldShowDeck(int characterId, CharacterDeckSave deck)
    {
        if (deck == null)
        {
            return false;
        }

        return deck.isRunSavedDeck && !HasStarterDeckCardIds(characterId, deck.cardIds);
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

    private static bool HasStarterDeckCardIds(int characterId, List<int> cardIds)
    {
        List<int> starterCardIds = CharacterManager.GetStarterCardIds(characterId);
        if (starterCardIds == null || cardIds == null || starterCardIds.Count != cardIds.Count)
        {
            return false;
        }

        for (int i = 0; i < starterCardIds.Count; i++)
        {
            if (starterCardIds[i] != cardIds[i])
            {
                return false;
            }
        }

        return true;
    }

    private static string CreateDeckLabel(int characterId, CharacterDeckSave deck)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("직업: ");
        builder.Append(ResolveCharacterName(characterId));
        builder.AppendLine();
        builder.Append("덱: ");
        builder.Append(string.IsNullOrWhiteSpace(deck.name) ? DefaultDeckName : deck.name);
        builder.AppendLine();
        builder.Append("카드 ID: ");
        AppendCardIds(builder, deck.cardIds);
        return builder.ToString();
    }

    private static string ResolveCharacterName(int characterId)
    {
        CharacterData characterData = CharacterManager.GetCharacter(characterId);
        if (characterData != null && !string.IsNullOrWhiteSpace(characterData.characterName))
        {
            return characterData.characterName;
        }

        if (Enum.IsDefined(typeof(Character), characterId))
        {
            return ((Character)characterId).ToString();
        }

        return characterId.ToString();
    }

    private static void AppendCardIds(StringBuilder builder, List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0)
        {
            builder.Append("없음");
            return;
        }

        for (int i = 0; i < cardIds.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(cardIds[i]);
        }
    }

    private static void SetButtonText(GameObject buttonObject, string label)
    {
        TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = label ?? string.Empty;
            return;
        }

        Text legacyText = buttonObject.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label ?? string.Empty;
            return;
        }

        Debug.LogWarning("[RankingModeManager] 버튼 프리팹 자식에서 텍스트 컴포넌트를 찾을 수 없습니다");
    }
}
