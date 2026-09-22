using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingModeSavedDeckPanel : MonoBehaviour
{
    private const int MaxDeckPanelCount = 5;

    [Header("저장덱 패널 설정")]
    [SerializeField] private RectTransform[] cardPanels = new RectTransform[MaxDeckPanelCount];
    [SerializeField] private GameObject cardPrefab;

    [Header("저장덱 이름 변경 설정")]
    [SerializeField] private TMP_InputField[] renameInputFields = new TMP_InputField[MaxDeckPanelCount];

    private sealed class DeckPanelBinding
    {
        public RectTransform panel;
        public DictionaryCardLayout cardLayout;
        public readonly List<GameObject> createdCards = new List<GameObject>();
        public TMP_InputField renameInputField;
        public Character character;
        public CharacterDeckSave deck;
    }

    private readonly List<DeckPanelBinding> panelBindings = new List<DeckPanelBinding>();
    private DeckPanelBinding activeRenameBinding;
    private TMP_InputField activeRenameInputField;

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
        ClearPanelBindings();
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
    }

    public void DeleteDeck(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panelBindings.Count)
        {
            Debug.LogWarning($"[RankingModeSavedDeckPanel] 삭제할 저장덱 패널 인덱스가 올바르지 않습니다: {panelIndex}");
            return;
        }

        DeckPanelBinding binding = panelBindings[panelIndex];
        if (binding?.deck == null)
        {
            return;
        }

        PlayerProfileSave profile = ProfileSaveManager.CurrentProfile;
        CharacterDeckLibrarySave library = profile?.FindLibrary(CharacterManager.GetIdByCharacterEnum(binding.character));
        if (library?.decks == null)
        {
            Debug.LogWarning("[RankingModeSavedDeckPanel] 삭제할 저장덱 라이브러리를 찾을 수 없습니다");
            return;
        }

        CharacterDeckSave deleteTarget = binding.deck;
        bool removed = false;
        for (int i = library.decks.Count - 1; i >= 0; i--)
        {
            if (!IsSameDeck(library.decks[i], deleteTarget))
            {
                continue;
            }

            library.decks.RemoveAt(i);
            removed = true;
            break;
        }

        if (!removed)
        {
            Debug.LogWarning("[RankingModeSavedDeckPanel] 삭제할 저장덱을 찾을 수 없습니다");
            return;
        }

        if (!string.IsNullOrWhiteSpace(deleteTarget.deckId)
            && string.Equals(library.selectedDeckId, deleteTarget.deckId, StringComparison.Ordinal))
        {
            library.selectedDeckId = string.Empty;
        }

        ProfileSaveManager.Save(profile);
        Debug.Log($"[RankingModeSavedDeckPanel] 저장덱을 삭제했습니다: {ResolveDeckName(deleteTarget)}");
        ShowSavedDecks(binding.character);
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

        foreach (RectTransform panel in cardPanels)
        {
            if (panel == null)
            {
                continue;
            }

            DictionaryCardLayout layout = panel.GetComponent<DictionaryCardLayout>();
            if (layout == null || !layout.HasCardPositions)
            {
                Debug.LogError("[RankingModeSavedDeckPanel] 씬 카드 배치 위치가 연결되지 않았습니다", panel);
                return false;
            }
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

            DeckPanelBinding binding = new DeckPanelBinding
            {
                panel = panel,
                cardLayout = panel.GetComponent<DictionaryCardLayout>(),
                renameInputField = renameInputFields != null && i < renameInputFields.Length ? renameInputFields[i] : null
            };

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

        foreach (GameObject card in binding.createdCards)
        {
            if (card != null)
            {
                card.SetActive(false);
                Destroy(card);
            }
        }

        binding.createdCards.Clear();
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
        if (deck?.cardIds == null || deck.cardIds.Count != DictionaryCardLayout.RequiredCardCount)
        {
            throw new InvalidOperationException($"[RankingModeSavedDeckPanel] 저장덱은 정확히 {DictionaryCardLayout.RequiredCardCount}장이어야 합니다: {ResolveDeckName(deck)}");
        }

        if (binding?.cardLayout == null)
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

            CreateCard(binding, card, i);
        }
    }

    private void CreateCard(DeckPanelBinding binding, Card card, int cardIndex)
    {
        GameObject cardObject = Instantiate(cardPrefab, binding.cardLayout.transform, false);
        cardObject.name = $"RankingModeSavedDeckCard_{card.cardId}";
        DisableCardRaycasts(cardObject);

        RectTransform cardRect = cardObject.transform as RectTransform;
        if (!binding.cardLayout.PlaceCard(cardRect, cardIndex))
        {
            cardObject.SetActive(false);
            Destroy(cardObject);
            return;
        }

        binding.createdCards.Add(cardObject);
        SetupCardController(cardObject, card);
        CaptureCardScale(cardObject);
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
