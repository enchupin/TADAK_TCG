using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RankingModeSavedDeckPanel : MonoBehaviour
{
    private const int MaxDeckPanelCount = 5;
    private const int RequiredSelectionCount = 3;
    private const string CardRootName = "RankingModeSavedDeckCardRoot";
    private const float CardAreaHeightRatio = 0.8f;

    [Header("저장덱 패널 설정")]
    [SerializeField] private RectTransform[] cardPanels = new RectTransform[MaxDeckPanelCount];
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float cardScale = 0.28f;
    [SerializeField] private Vector2 cardSpacing = new Vector2(6f, 0f);
    [SerializeField] private Vector2 cardPadding = new Vector2(8f, 0f);
    [SerializeField] private Color selectedPanelColor = new Color(0.35f, 0.85f, 1f, 1f);

    [Header("저장덱 이름 변경 설정")]
    [SerializeField] private TMP_InputField[] renameInputFields = new TMP_InputField[MaxDeckPanelCount];

    private sealed class DeckPanelBinding
    {
        public RectTransform panel;
        public Image panelImage;
        public Color defaultColor;
        public RectTransform cardRoot;
        public TMP_InputField renameInputField;
        public Character character;
        public CharacterDeckSave deck;
    }

    private readonly List<DeckPanelBinding> panelBindings = new List<DeckPanelBinding>();
    private readonly Dictionary<RectTransform, Color> defaultPanelColors = new Dictionary<RectTransform, Color>();
    private readonly Dictionary<Character, CharacterDeckSave> selectedDecksByCharacter = new Dictionary<Character, CharacterDeckSave>();
    private DeckPanelBinding activeRenameBinding;
    private TMP_InputField activeRenameInputField;

    public int SelectedDeckCount => selectedDecksByCharacter.Count;
    public bool HasRequiredSelectionCount => selectedDecksByCharacter.Count == RequiredSelectionCount;

    private void Awake()
    {
        HideRenameInputFields();
    }

    private void OnDestroy()
    {
        UnbindActiveRenameInput();
    }

    public void ShowSavedDecks(Character character)
    {
        if (!ValidateReferences())
        {
            return;
        }

        CancelRename();
        PreparePanelBindings();
        ClearPanelBindings();

        List<CharacterDeckSave> decks = CollectDecks(character);
        int displayCount = Mathf.Min(MaxDeckPanelCount, decks.Count, panelBindings.Count);

        for (int i = 0; i < displayCount; i++)
        {
            BindDeckToPanel(panelBindings[i], character, decks[i]);
        }

        if (decks.Count > MaxDeckPanelCount)
        {
            Debug.LogWarning($"[RankingModeSavedDeckPanel] 표시 가능한 저장덱 수를 초과했습니다: {decks.Count}/{MaxDeckPanelCount}");
        }

        RefreshSelectionView();
    }

    public List<CharacterDeckSave> CopySelectedDecks()
    {
        return new List<CharacterDeckSave>(selectedDecksByCharacter.Values);
    }

    public bool TryGetSelectedDeck(Character character, out CharacterDeckSave deck)
    {
        return selectedDecksByCharacter.TryGetValue(character, out deck);
    }

    private bool ValidateReferences()
    {
        if (cardPanels == null || cardPanels.Length == 0)
        {
            Debug.LogError("[RankingModeSavedDeckPanel] 저장덱 CardPanel이 연결되지 않았습니다");
            return false;
        }

        if (cardPrefab == null)
        {
            Debug.LogError("[RankingModeSavedDeckPanel] 카드 프리팹이 연결되지 않았습니다");
            return false;
        }

        return true;
    }

    private void PreparePanelBindings()
    {
        panelBindings.Clear();
        int panelCount = Mathf.Min(MaxDeckPanelCount, cardPanels.Length);
        for (int i = 0; i < panelCount; i++)
        {
            RectTransform panel = cardPanels[i];
            if (panel == null)
            {
                continue;
            }

            Image panelImage = ResolvePanelImage(panel);
            DisablePanelChildRaycasts(panel, panelImage);
            if (!defaultPanelColors.TryGetValue(panel, out Color defaultColor))
            {
                defaultColor = panelImage != null ? panelImage.color : Color.white;
                defaultPanelColors[panel] = defaultColor;
            }

            DeckPanelBinding binding = new DeckPanelBinding
            {
                panel = panel,
                panelImage = panelImage,
                defaultColor = defaultColor,
                cardRoot = ResolveCardRoot(panel),
                renameInputField = renameInputFields != null && i < renameInputFields.Length ? renameInputFields[i] : null
            };

            BindPanelClick(panel, binding);
            panelBindings.Add(binding);
        }
    }

    private void ClearPanelBindings()
    {
        foreach (DeckPanelBinding binding in panelBindings)
        {
            ClearPanel(binding);
        }
    }

    private void ClearPanel(DeckPanelBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        binding.character = Character.Monster;
        binding.deck = null;
        SetPanelText(binding.panel, string.Empty);
        if (binding.renameInputField != null)
        {
            binding.renameInputField.SetTextWithoutNotify(string.Empty);
            binding.renameInputField.gameObject.SetActive(false);
        }

        if (binding.panelImage != null)
        {
            binding.panelImage.color = binding.defaultColor;
        }

        if (binding.cardRoot == null)
        {
            return;
        }

        for (int i = binding.cardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(binding.cardRoot.GetChild(i).gameObject);
        }
    }

    private List<CharacterDeckSave> CollectDecks(Character character)
    {
        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        CharacterDeckLibrarySave library = profile?.FindLibrary(CharacterManager.GetIdByCharacterEnum(character));
        List<CharacterDeckSave> decks = new List<CharacterDeckSave>();
        if (library?.decks == null)
        {
            return decks;
        }

        foreach (CharacterDeckSave deck in library.decks)
        {
            if (deck != null)
            {
                decks.Add(deck);
            }
        }

        return decks;
    }

    private void BindDeckToPanel(DeckPanelBinding binding, Character character, CharacterDeckSave deck)
    {
        if (binding == null || deck == null)
        {
            return;
        }

        binding.character = character;
        binding.deck = deck;
        SetPanelText(binding.panel, ResolveDeckName(deck));
        if (binding.renameInputField != null)
        {
            binding.renameInputField.SetTextWithoutNotify(ResolveDeckName(deck));
            binding.renameInputField.gameObject.SetActive(false);
        }

        CreateDeckCards(binding, deck);
    }

    private void CreateDeckCards(DeckPanelBinding binding, CharacterDeckSave deck)
    {
        if (binding?.cardRoot == null || deck?.cardIds == null)
        {
            return;
        }

        List<int> sortedCardIds = new List<int>(deck.cardIds);
        sortedCardIds.Sort();

        for (int i = 0; i < sortedCardIds.Count; i++)
        {
            Card card = CardManager.GetCardAsCard(sortedCardIds[i]);
            if (card == null)
            {
                Debug.LogWarning($"[RankingModeSavedDeckPanel] 카드 데이터를 찾을 수 없습니다: {sortedCardIds[i]}");
                continue;
            }

            CreateCard(binding.cardRoot, card, i, sortedCardIds.Count);
        }
    }

    private void CreateCard(RectTransform cardRoot, Card card, int cardIndex, int cardCount)
    {
        GameObject cardObject = Instantiate(cardPrefab, cardRoot, false);
        cardObject.name = $"RankingModeSavedDeckCard_{card.cardId}";
        DisableCardRaycasts(cardObject);

        RectTransform cardRect = cardObject.transform as RectTransform;
        Vector2 cardSize = ResolveCardSize(cardRect);
        Vector2 rootSize = ResolveRectSize(cardRoot);
        float resolvedScale = ResolveCardScale(cardSize, rootSize, cardCount);
        SetupCardTransform(cardRect, cardIndex, cardSize, resolvedScale);
        SetupCardController(cardObject, card);
        CaptureCardScale(cardObject);
    }

    private Vector2 ResolveCardSize(RectTransform cardRect)
    {
        if (cardRect == null)
        {
            return new Vector2(200f, 280f);
        }

        Vector2 size = cardRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = cardRect.sizeDelta;
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            return new Vector2(200f, 280f);
        }

        return size;
    }

    private Vector2 ResolveRectSize(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return Vector2.zero;
        }

        Vector2 size = rectTransform.rect.size;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rectTransform.sizeDelta;
        }

        if ((size.x <= 0f || size.y <= 0f) && rectTransform.parent is RectTransform parentRect)
        {
            Vector2 parentSize = ResolveRectSize(parentRect);
            Vector2 anchorSize = rectTransform.anchorMax - rectTransform.anchorMin;
            size = new Vector2(
                size.x > 0f ? size.x : parentSize.x * anchorSize.x,
                size.y > 0f ? size.y : parentSize.y * anchorSize.y);
        }

        return size;
    }

    private float ResolveCardScale(Vector2 cardSize, Vector2 rootSize, int cardCount)
    {
        if (cardCount <= 0 || cardSize.x <= 0f || cardSize.y <= 0f || rootSize.x <= 0f || rootSize.y <= 0f)
        {
            return Mathf.Max(0.01f, cardScale);
        }

        float availableWidth = rootSize.x - cardPadding.x * 2f - cardSpacing.x * Mathf.Max(0, cardCount - 1);
        float availableHeight = rootSize.y - cardPadding.y * 2f;
        float widthScale = availableWidth / (cardSize.x * cardCount);
        float heightScale = availableHeight / cardSize.y;
        float fitScale = Mathf.Min(widthScale, heightScale);

        return Mathf.Max(0.01f, Mathf.Min(cardScale, fitScale));
    }

    private void SetupCardTransform(RectTransform cardRect, int cardIndex, Vector2 cardSize, float resolvedScale)
    {
        if (cardRect == null)
        {
            return;
        }

        float scaledWidth = cardSize.x * resolvedScale;
        cardRect.anchorMin = new Vector2(0f, 0.5f);
        cardRect.anchorMax = new Vector2(0f, 0.5f);
        cardRect.pivot = new Vector2(0f, 0.5f);
        cardRect.anchoredPosition = new Vector2(
            cardPadding.x + cardIndex * (scaledWidth + cardSpacing.x),
            0f);
        cardRect.localScale = Vector3.one * resolvedScale;
    }

    private void SetupCardController(GameObject cardObject, Card card)
    {
        CardController controller = cardObject.GetComponent<CardController>();
        if (controller != null)
        {
            controller.useInteractionHandler = false;
            if (controller.interactionHandler != null)
            {
                controller.interactionHandler.showPlayThreshold = false;
                controller.interactionHandler.enabled = false;
            }

            controller.Initialize(card);
            return;
        }

        CardUI cardUI = cardObject.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.UpdateDisplay(card);
            return;
        }

        Debug.LogWarning("[RankingModeSavedDeckPanel] 카드 프리팹에서 CardController 또는 CardUI를 찾을 수 없습니다");
    }

    private void CaptureCardScale(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }

        UIHoverEffect[] hoverEffects = cardObject.GetComponentsInChildren<UIHoverEffect>(true);
        foreach (UIHoverEffect hoverEffect in hoverEffects)
        {
            if (hoverEffect == null)
            {
                continue;
            }

            hoverEffect.StopAnimation();
            hoverEffect.CaptureCurrentScaleAsOriginal();
        }
    }

    private void DisableCardRaycasts(GameObject cardObject)
    {
        if (cardObject == null)
        {
            return;
        }

        Graphic[] graphics = cardObject.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    private void DisablePanelChildRaycasts(RectTransform panel, Graphic panelGraphic)
    {
        if (panel == null)
        {
            return;
        }

        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || graphic == panelGraphic || graphic.GetComponentInParent<Selectable>(true) != null)
            {
                continue;
            }

            graphic.raycastTarget = false;
        }
    }

    private void SelectDeck(DeckPanelBinding binding)
    {
        if (binding?.deck == null)
        {
            return;
        }

        if (selectedDecksByCharacter.TryGetValue(binding.character, out CharacterDeckSave selectedDeck)
            && IsSameDeck(selectedDeck, binding.deck))
        {
            selectedDecksByCharacter.Remove(binding.character);
            RefreshSelectionView();
            return;
        }

        if (!selectedDecksByCharacter.ContainsKey(binding.character)
            && selectedDecksByCharacter.Count >= RequiredSelectionCount)
        {
            Debug.LogWarning("[RankingModeSavedDeckPanel] 랭킹모드는 저장덱 3개까지만 선택할 수 있습니다");
            return;
        }

        selectedDecksByCharacter[binding.character] = binding.deck;
        RefreshSelectionView();
    }

    private void RefreshSelectionView()
    {
        foreach (DeckPanelBinding binding in panelBindings)
        {
            if (binding?.panelImage == null)
            {
                continue;
            }

            bool isSelected = binding.deck != null
                && selectedDecksByCharacter.TryGetValue(binding.character, out CharacterDeckSave selectedDeck)
                && IsSameDeck(selectedDeck, binding.deck);
            binding.panelImage.color = isSelected ? selectedPanelColor : binding.defaultColor;
        }
    }

    private bool IsSameDeck(CharacterDeckSave left, CharacterDeckSave right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(left.deckId) || !string.IsNullOrWhiteSpace(right.deckId))
        {
            return string.Equals(left.deckId, right.deckId, StringComparison.Ordinal);
        }

        return ReferenceEquals(left, right);
    }

    private Image ResolvePanelImage(RectTransform panel)
    {
        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = panel.gameObject.AddComponent<Image>();
            image.color = Color.clear;
        }

        image.raycastTarget = true;
        return image;
    }

    private RectTransform ResolveCardRoot(RectTransform panel)
    {
        Transform existingRoot = panel.Find(CardRootName);
        RectTransform root = existingRoot as RectTransform;
        if (root == null)
        {
            GameObject rootObject = new GameObject(CardRootName, typeof(RectTransform));
            root = rootObject.transform as RectTransform;
            root.SetParent(panel, false);
        }

        root.anchorMin = Vector2.zero;
        root.anchorMax = new Vector2(1f, CardAreaHeightRatio);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;
        return root;
    }

    private void BindPanelClick(RectTransform panel, DeckPanelBinding binding)
    {
        RankingModeDeckPanelClickHandler clickHandler = panel.GetComponent<RankingModeDeckPanelClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = panel.gameObject.AddComponent<RankingModeDeckPanelClickHandler>();
        }

        clickHandler.Bind(() => SelectDeck(binding));
    }

    public void BeginRename(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panelBindings.Count)
        {
            Debug.LogWarning($"[RankingModeSavedDeckPanel] 이름을 변경할 저장덱 패널 인덱스가 올바르지 않습니다: {panelIndex}");
            return;
        }

        DeckPanelBinding binding = panelBindings[panelIndex];
        if (binding?.deck == null)
        {
            return;
        }

        if (binding.renameInputField == null)
        {
            Debug.LogWarning("[RankingModeSavedDeckPanel] 이름 변경 입력 필드가 연결되지 않았습니다");
            return;
        }

        CancelRename();
        activeRenameBinding = binding;
        activeRenameInputField = binding.renameInputField;
        activeRenameInputField.onEndEdit.RemoveListener(CompleteRenameFromInput);
        activeRenameInputField.onEndEdit.AddListener(CompleteRenameFromInput);
        activeRenameInputField.lineType = TMP_InputField.LineType.SingleLine;
        activeRenameInputField.SetTextWithoutNotify(ResolveDeckName(binding.deck));
        activeRenameInputField.gameObject.SetActive(true);

        SetDeckNameTextActive(binding.panel, false);
        activeRenameInputField.Select();
        activeRenameInputField.ActivateInputField();
        activeRenameInputField.MoveTextEnd(false);
    }

    public void ConfirmRename()
    {
        if (activeRenameInputField == null)
        {
            return;
        }

        CompleteRename(activeRenameInputField.text, activeRenameInputField.wasCanceled);
    }

    public void CancelRename()
    {
        if (activeRenameBinding == null && activeRenameInputField == null)
        {
            return;
        }

        DeckPanelBinding binding = activeRenameBinding;
        TMP_InputField inputField = activeRenameInputField;
        UnbindActiveRenameInput();
        activeRenameBinding = null;
        activeRenameInputField = null;

        if (binding != null)
        {
            SetDeckNameTextActive(binding.panel, true);
            if (binding.renameInputField != null)
            {
                binding.renameInputField.SetTextWithoutNotify(ResolveDeckName(binding.deck));
                binding.renameInputField.gameObject.SetActive(false);
            }

            return;
        }

        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
        }
    }

    private void CompleteRenameFromInput(string newName)
    {
        CompleteRename(newName, activeRenameInputField != null && activeRenameInputField.wasCanceled);
    }

    private void CompleteRename(string newName, bool isCanceled)
    {
        DeckPanelBinding binding = activeRenameBinding;
        if (binding?.deck == null)
        {
            CancelRename();
            return;
        }

        if (isCanceled)
        {
            CancelRename();
            return;
        }

        string deckName = string.IsNullOrWhiteSpace(newName)
            ? string.Empty
            : newName.Replace("\r", " ").Replace("\n", " ").Trim();
        if (string.IsNullOrWhiteSpace(deckName))
        {
            Debug.LogWarning("[RankingModeSavedDeckPanel] 저장덱 이름은 비워둘 수 없습니다");
            if (binding.renameInputField != null)
            {
                binding.renameInputField.SetTextWithoutNotify(ResolveDeckName(binding.deck));
                binding.renameInputField.gameObject.SetActive(true);
            }

            activeRenameInputField?.ActivateInputField();
            return;
        }

        UnbindActiveRenameInput();
        activeRenameBinding = null;
        activeRenameInputField = null;

        if (!string.Equals(binding.deck.name, deckName, StringComparison.Ordinal))
        {
            binding.deck.name = deckName;
            ProfileSaveManager.Save();
            Debug.Log($"[RankingModeSavedDeckPanel] 저장덱 이름을 변경했습니다: {deckName}");
        }

        SetPanelText(binding.panel, ResolveDeckName(binding.deck));
        SetDeckNameTextActive(binding.panel, true);
        if (binding.renameInputField != null)
        {
            binding.renameInputField.SetTextWithoutNotify(ResolveDeckName(binding.deck));
            binding.renameInputField.gameObject.SetActive(false);
        }
    }

    private void HideRenameInputFields()
    {
        if (renameInputFields == null)
        {
            return;
        }

        foreach (TMP_InputField inputField in renameInputFields)
        {
            if (inputField != null)
            {
                inputField.gameObject.SetActive(false);
            }
        }
    }

    private void UnbindActiveRenameInput()
    {
        if (activeRenameInputField == null)
        {
            return;
        }

        activeRenameInputField.onEndEdit.RemoveListener(CompleteRenameFromInput);
        activeRenameInputField.DeactivateInputField();
    }

    private void SetDeckNameTextActive(RectTransform panel, bool isActive)
    {
        TMP_Text tmpText = FindDeckNameText<TMP_Text>(panel);
        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(isActive);
            return;
        }

        Text legacyText = FindDeckNameText<Text>(panel);
        if (legacyText != null)
        {
            legacyText.gameObject.SetActive(isActive);
        }
    }

    private void SetPanelText(RectTransform panel, string text)
    {
        TMP_Text tmpText = FindDeckNameText<TMP_Text>(panel);
        if (tmpText != null)
        {
            tmpText.text = text ?? string.Empty;
            return;
        }

        Text legacyText = FindDeckNameText<Text>(panel);
        if (legacyText != null)
        {
            legacyText.text = text ?? string.Empty;
        }
    }

    private T FindDeckNameText<T>(RectTransform panel) where T : Component
    {
        if (panel == null)
        {
            return null;
        }

        T[] texts = panel.GetComponentsInChildren<T>(true);
        foreach (T text in texts)
        {
            if (text == null || text.gameObject.name != "DeckName")
            {
                continue;
            }

            return text;
        }

        return null;
    }

    private string ResolveDeckName(CharacterDeckSave deck)
    {
        if (deck == null || string.IsNullOrWhiteSpace(deck.name))
        {
            return "이름 없는 덱";
        }

        return deck.name;
    }
}

public class RankingModeDeckPanelClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Action onClicked;

    public void Bind(Action clickAction)
    {
        onClicked = clickAction;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        onClicked?.Invoke();
    }
}
