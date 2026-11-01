using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 보스 모드 진입: 캐릭터 3명 선택 → 캐릭터마다 저장 덱 선택(팝업) → 보스 선택 → 시작
/// 고른 덱은 캐릭터 버튼 오른쪽 확장 영역에 카드로 표시
/// </summary>
// 덱 선택 · 보스 선택은 각각 별도 단계 팝업 (검은 차단막, ESC · 바깥 클릭 = 이전 단계)
// 보스 선택 팝업은 3명의 덱 선택이 끝나면 열리고, 이전 단계로 가면 고른 보스 초기화
public class BossModeDeckSelection : MonoBehaviour
{
    [Header("캐릭터 버튼 (BossModeCharacterButton 프리팹으로 생성)")]
    [SerializeField] private RectTransform characterContent;
    [SerializeField] private GameObject characterButtonPrefab;
    [Tooltip("첫 버튼 중심 위치")]
    [SerializeField] private Vector2 firstButtonPosition = new Vector2(190f, 0f);
    [Tooltip("버튼 중심 간 거리 (확장된 버튼 뒤로는 확장 폭만큼 더 띄움)")]
    [SerializeField] private float buttonSpacing = 350f;
    [Tooltip("마지막 버튼 뒤 여백")]
    [SerializeField] private float trailingPadding = 30f;

    [Header("덱 선택 팝업 (도감 DeckPanel 복제, 배경 없이 덱 5칸만)")]
    [SerializeField] private GameObject deckPopupRoot;
    [Tooltip("팝업 안 저장 덱 패널 (읽기 전용으로 설정)")]
    [SerializeField] private DictionarySavedDeckPanel savedDeckPanel;
    [SerializeField] private BossModeDeckSlot[] deckSlots;

    [Header("보스 선택 팝업 (캐릭터 3명의 덱 선택이 끝나면 열림)")]
    [SerializeField] private GameObject bossPopupRoot;
    [Tooltip("아라크네, 선지자, 얼음과 불, 공허 군주 순서로 연결")]
    [SerializeField] private Button[] bossButtons;

    [Header("시작 (보스 팝업 안에 두는 것을 권장)")]
    [SerializeField] private Button startButton;
    [SerializeField] private string battleSceneName = "CombatScene";

    private readonly BossModeSelectionState state = new BossModeSelectionState();
    private readonly List<BossModeCharacterButton> characterButtons = new List<BossModeCharacterButton>();
    private bool starting;
    private int selectedBossId;
    private bool isPopupRegistered;
    private bool isBossPopupRegistered;
    private ScrollRect scrollRect;
    // OnClick이 비어 있는 보스 버튼에 코드로 연결한 클릭 (패널을 닫을 때 해제)
    private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bossButtonBindings = new List<(Button, UnityEngine.Events.UnityAction)>();

    private void OnEnable()
    {
        state.Clear();
        selectedBossId = 0;
        starting = false;
        RegisterDeckPopup();
        RegisterBossPopup();
        HideDeckPopup();
        HideBossPopup();
        if (startButton != null) startButton.onClick.AddListener(StartBattle);
        BindBossButtons();
        CreateCharacterButtons();
        Refresh();
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartBattle);
        UnbindBossButtons();
        state.Clear();
        HideDeckPopup();
        HideBossPopup();
    }

    // 팝업은 일반 패널: ESC · 바깥 클릭으로 닫으면 그 캐릭터 선택도 취소
    private void RegisterDeckPopup()
    {
        if (isPopupRegistered || deckPopupRoot == null)
        {
            return;
        }

        isPopupRegistered = true;
        UIModalPanel.Ensure(deckPopupRoot, new UIModalPanel.Options {
            close = CancelDeckPopup,
            closeOnDimmerClick = true
        });
    }

    // 보스 선택도 별도 단계 팝업: ESC · 바깥 클릭으로 캐릭터 선택 단계로 돌아감 (고른 보스 초기화)
    private void RegisterBossPopup()
    {
        if (isBossPopupRegistered || bossPopupRoot == null)
        {
            return;
        }

        isBossPopupRegistered = true;
        UIModalPanel.Ensure(bossPopupRoot, new UIModalPanel.Options {
            close = CancelBossPopup,
            closeOnDimmerClick = true
        });
    }

    public void CancelBossPopup()
    {
        selectedBossId = 0;
        HideBossPopup();
        Refresh();
    }

    private void ShowBossPopupIfReady()
    {
        if (bossPopupRoot == null || state.SelectedCount != BossModeSelectionState.RequiredCharacterCount || state.PendingCharacter.HasValue)
        {
            return;
        }

        selectedBossId = 0;
        bossPopupRoot.SetActive(true);
    }

    private void HideBossPopup()
    {
        if (bossPopupRoot != null && bossPopupRoot.activeSelf)
        {
            bossPopupRoot.SetActive(false);
        }
    }

    private void CreateCharacterButtons()
    {
        if (characterButtons.Count > 0)
        {
            return;
        }

        if (characterContent == null || characterButtonPrefab == null)
        {
            Debug.LogError("[BossModeDeckSelection] 캐릭터 버튼 Content 또는 프리팹이 연결되지 않았습니다", this);
            return;
        }

        SetupScroll();

        List<CharacterData> characters = CharacterManager.GetAllCharacters();
        if (characters == null)
        {
            return;
        }

        characters.Sort((left, right) => left.characterId.CompareTo(right.characterId));
        foreach (CharacterData characterData in characters)
        {
            if (characterData == null || !System.Enum.IsDefined(typeof(Character), characterData.characterId))
            {
                continue;
            }

            GameObject buttonObject = Instantiate(characterButtonPrefab, characterContent, false);
            buttonObject.name = $"BossCharacterButton_{characterData.characterId}";
            if (!buttonObject.TryGetComponent(out BossModeCharacterButton characterButton))
            {
                Debug.LogError("[BossModeDeckSelection] 캐릭터 버튼 프리팹에 BossModeCharacterButton이 없습니다", buttonObject);
                Destroy(buttonObject);
                return;
            }

            characterButton.Initialize(characterData, OnCharacterClicked);
            characterButtons.Add(characterButton);
        }
    }

    // 보스 버튼 순서대로 301 ~ 304 선택 (인스펙터 OnClick에 SelectBoss를 직접 연결한 버튼은 그대로 사용)
    private void BindBossButtons()
    {
        if (bossButtons == null)
        {
            return;
        }

        for (int i = 0; i < bossButtons.Length; i++)
        {
            Button button = bossButtons[i];
            if (button == null || button.onClick.GetPersistentEventCount() > 0)
            {
                continue;
            }

            int bossId = 301 + i;
            UnityEngine.Events.UnityAction action = () => SelectBoss(bossId);
            button.onClick.AddListener(action);
            bossButtonBindings.Add((button, action));
        }
    }

    private void UnbindBossButtons()
    {
        foreach ((Button button, UnityEngine.Events.UnityAction action) in bossButtonBindings)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        bossButtonBindings.Clear();
    }

    // Scroll View를 연결한 경우 그 Content를 사용, 가로 스크롤은 휠로도 움직이게 함 (훈련 모드와 같은 방식)
    private void SetupScroll()
    {
        scrollRect = characterContent.GetComponentInParent<ScrollRect>();
        if (characterContent.TryGetComponent(out ScrollRect connectedScroll) && connectedScroll.content != null)
        {
            Debug.LogWarning("[BossModeDeckSelection] characterContent에는 Scroll View가 아니라 Viewport/Content를 연결해야 합니다", this);
            scrollRect = connectedScroll;
            characterContent = connectedScroll.content;
        }

        if (scrollRect == null)
        {
            return;
        }

        RectTransform wheelTarget = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        ScrollRectWheelRouter wheelRouter = wheelTarget.GetComponent<ScrollRectWheelRouter>();
        if (wheelRouter == null)
        {
            wheelRouter = wheelTarget.gameObject.AddComponent<ScrollRectWheelRouter>();
        }
        wheelRouter.Bind(scrollRect);
        wheelRouter.enabled = true;
    }

    private void OnCharacterClicked(Character character)
    {
        if (starting)
        {
            return;
        }

        BossModeSelectionState.ClickResult result = state.Click(character, CollectDeckIds(character));
        if (result == BossModeSelectionState.ClickResult.OpenDeckPopup)
        {
            ShowDeckPopup(character);
        }
        else if (result == BossModeSelectionState.ClickResult.SelectedDirectly)
        {
            ShowBossPopupIfReady();
        }

        Refresh();
    }

    // BossModeDeckSlot 클릭: 팝업에서 덱 선택 완료
    public void ChooseDeck(int panelIndex)
    {
        if (starting || savedDeckPanel == null || !state.PendingCharacter.HasValue
            || !savedDeckPanel.TryGetDisplayedDeck(panelIndex, out _, out CharacterDeckSave deck))
        {
            return;
        }

        if (state.ChooseDeck(deck.deckId))
        {
            HideDeckPopup();
            ShowBossPopupIfReady();
            Refresh();
        }
    }

    // 덱을 고르지 않고 팝업을 닫음 (ESC · 바깥 클릭)
    public void CancelDeckPopup()
    {
        state.CancelPending();
        HideDeckPopup();
        Refresh();
    }

    public void SelectBoss(int monsterId)
    {
        if (starting || !BossModeSession.IsValidBoss(monsterId)) return;
        selectedBossId = selectedBossId == monsterId ? 0 : monsterId;
        Refresh();
    }

    private void ShowDeckPopup(Character character)
    {
        if (deckPopupRoot == null || savedDeckPanel == null)
        {
            Debug.LogError("[BossModeDeckSelection] 덱 선택 팝업이 연결되지 않았습니다", this);
            state.CancelPending();
            return;
        }

        savedDeckPanel.ShowSavedDecks(character);
        deckPopupRoot.SetActive(true);
        RefreshDeckSlots();
    }

    private void HideDeckPopup()
    {
        if (deckPopupRoot != null && deckPopupRoot.activeSelf)
        {
            deckPopupRoot.SetActive(false);
        }
    }

    private static List<string> CollectDeckIds(Character character)
    {
        List<string> deckIds = new List<string>();
        List<CharacterDeckSave> decks = ProfileSaveManager.CurrentProfile?.FindLibrary((int)character)?.decks;
        if (decks == null)
        {
            return deckIds;
        }

        foreach (CharacterDeckSave deck in decks)
        {
            if (deck != null && !string.IsNullOrEmpty(deck.deckId))
            {
                deckIds.Add(deck.deckId);
            }
        }

        return deckIds;
    }

    private static CharacterDeckSave ResolveDeck(Character character, string deckId)
    {
        return ProfileSaveManager.CurrentProfile?.FindLibrary((int)character)?.decks?.Find(deck => deck != null && deck.deckId == deckId);
    }

    private void Refresh()
    {
        state.RemoveMissing((character, deckId) => ResolveDeck(character, deckId) != null);

        foreach (BossModeCharacterButton characterButton in characterButtons)
        {
            bool selected = state.TryGetDeckId(characterButton.Character, out string deckId);
            characterButton.SetState(selected, selected ? ResolveDeck(characterButton.Character, deckId) : null);
        }

        LayoutCharacterButtons();
        RefreshDeckSlots();

        if (bossButtons != null) {
            for (int i = 0; i < bossButtons.Length; i++) {
                Button button = bossButtons[i];
                if (button == null) continue;
                ColorBlock colors = button.colors;
                Color color = selectedBossId == 301 + i ? new Color(1f, 0.8f, 0.35f) : Color.white;
                colors.normalColor = color;
                colors.selectedColor = color;
                colors.highlightedColor = color;
                button.colors = colors;
            }
        }
        if (startButton != null) startButton.interactable = !starting && state.CanStart(selectedBossId, BossModeSession.IsValidBoss);
    }

    // 팝업에서 이 캐릭터가 이미 고른 덱 강조
    private void RefreshDeckSlots()
    {
        if (deckSlots == null)
        {
            return;
        }

        string currentDeckId = null;
        if (state.PendingCharacter.HasValue)
        {
            state.TryGetDeckId(state.PendingCharacter.Value, out currentDeckId);
        }

        for (int i = 0; i < deckSlots.Length; i++)
        {
            bool selected = currentDeckId != null && savedDeckPanel != null
                && savedDeckPanel.TryGetDisplayedDeck(i, out _, out CharacterDeckSave deck) && deck.deckId == currentDeckId;
            deckSlots[i]?.SetSelected(selected);
        }
    }

    // 확장된 버튼 뒤의 버튼은 확장 폭만큼 오른쪽으로
    private void LayoutCharacterButtons()
    {
        if (characterContent == null || characterButtons.Count == 0)
        {
            return;
        }

        float x = firstButtonPosition.x;
        float lastRight = 0f;
        foreach (BossModeCharacterButton characterButton in characterButtons)
        {
            RectTransform rect = characterButton.RectTransform;
            rect.anchoredPosition = new Vector2(x, firstButtonPosition.y);
            rect.localScale = Vector3.one;
            lastRight = x + rect.rect.width * 0.5f + characterButton.ExtraWidth;
            x += buttonSpacing + characterButton.ExtraWidth;
        }

        RectTransform viewport = scrollRect != null && scrollRect.viewport != null ? scrollRect.viewport : characterContent.parent as RectTransform;
        float minWidth = viewport != null ? viewport.rect.width : 0f;
        characterContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(minWidth, lastRight + trailingPadding));
    }

    public void StartBattle()
    {
        Refresh();
        if (starting || !state.CanStart(selectedBossId, BossModeSession.IsValidBoss)) return;
        List<Character> characters = new List<Character>(state.Selections.Keys);
        characters.Sort();
        List<int> cards = new List<int>();
        foreach (Character character in characters) {
            state.TryGetDeckId(character, out string deckId);
            CharacterDeckSave deck = ResolveDeck(character, deckId);
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
        BossModeSession.Begin(characters, cards, selectedBossId);
        SceneManager.LoadScene(battleSceneName);
    }
}
