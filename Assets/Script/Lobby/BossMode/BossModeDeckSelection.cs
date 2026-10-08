using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BossModeDeckSelection : MonoBehaviour
{
    [SerializeField] private DictionarySavedDeckPanel savedDeckPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private BossModeDeckSlot[] deckSlots;
    [SerializeField] private string battleSceneName = "CombatScene";
    private readonly Dictionary<Character, string> selections = new Dictionary<Character, string>();
    private bool starting;

    private void OnEnable()
    {
        selections.Clear();
        starting = false;
        if (savedDeckPanel != null) savedDeckPanel.DecksChanged += Refresh;
        if (startButton != null) startButton.onClick.AddListener(StartBattle);
        Refresh();
    }

    private void OnDisable()
    {
        if (savedDeckPanel != null) savedDeckPanel.DecksChanged -= Refresh;
        if (startButton != null) startButton.onClick.RemoveListener(StartBattle);
    }

    public void ToggleDeck(int panelIndex)
    {
        if (starting || savedDeckPanel == null || !savedDeckPanel.TryGetDisplayedDeck(panelIndex, out Character character, out CharacterDeckSave deck)) return;
        if (selections.TryGetValue(character, out string selectedId) && selectedId == deck.deckId) {
            selections.Remove(character);
        }
        else if (selections.ContainsKey(character) || selections.Count < 3) {
            selections[character] = deck.deckId;
        }
        Refresh();
    }

    private CharacterDeckSave ResolveDeck(Character character, string deckId)
    {
        return ProfileSaveManager.CurrentProfile?.FindLibrary((int)character)?.decks?.Find(deck => deck != null && deck.deckId == deckId);
    }

    private void Refresh()
    {
        foreach (Character character in new List<Character>(selections.Keys)) {
            if (ResolveDeck(character, selections[character]) == null) selections.Remove(character);
        }
        if (deckSlots != null) {
            for (int i = 0; i < deckSlots.Length; i++) {
                bool selected = savedDeckPanel != null && savedDeckPanel.TryGetDisplayedDeck(i, out Character character, out CharacterDeckSave deck)
                    && selections.TryGetValue(character, out string id) && id == deck.deckId;
                deckSlots[i]?.SetSelected(selected);
            }
        }
        if (startButton != null) startButton.interactable = !starting && selections.Count == 3;
    }

    public void StartBattle()
    {
        Refresh();
        if (starting || selections.Count != 3) return;
        List<Character> characters = new List<Character>(selections.Keys);
        characters.Sort();
        List<int> cards = new List<int>();
        foreach (Character character in characters) {
            CharacterDeckSave deck = ResolveDeck(character, selections[character]);
            if (deck?.cardIds == null || !CharacterDeckSave.IsValidCardCount(deck.cardIds.Count)) return;
            foreach (int cardId in deck.cardIds) {
                if (CardManager.GetCardAsCard(cardId) == null) {
                    Debug.LogError($"[BossModeDeckSelection] 저장덱의 카드를 찾을 수 없습니다: {cardId}");
                    return;
                }
                cards.Add(cardId);
            }
        }
        if (!Application.CanStreamedLevelBeLoaded(battleSceneName)) {
            Debug.LogError($"[BossModeDeckSelection] 전투 씬을 불러올 수 없습니다: {battleSceneName}");
            return;
        }
        starting = true;
        if (startButton != null) startButton.interactable = false;
        BossModeSession.Begin(characters, cards);
        SceneManager.LoadScene(battleSceneName);
    }
}
