using UnityEngine;
using UnityEngine.UI;

public class DictionaryPanelController : MonoBehaviour
{
    // 가운데 구역 탭: 저장덱 목록 / 캐릭터 일러스트
    private enum CenterTab
    {
        Deck,
        Illustration
    }

    [SerializeField] private DictionaryStarterCardPanel starterCardPanel;
    [SerializeField] private DictionarySavedDeckPanel savedDeckPanel;

    [Header("가운데 구역 탭 (비우면 저장덱 패널 아래 DeckBtn · IllustBtn을 찾음)")]
    [SerializeField] private Button deckTabButton;
    [SerializeField] private Button illustrationTabButton;
    [Tooltip("일러스트 탭에서 캐릭터 일러스트를 표시할 이미지 (비우면 저장덱 패널 아래 CharacterIllust를 찾음)")]
    [SerializeField] private Image illustrationImage;

    [Header("카드 확인 패널 (비우면 이름이 DetailPanel인 자식을 찾음)")]
    [SerializeField] private GameObject cardDetailPanelRoot;
    [Tooltip("카드 확인에 쓸 카드 프리팹 (비우면 오른쪽 구역 카드 프리팹 사용)")]
    [SerializeField] private GameObject cardDetailCardPrefab;

    private CardDetailSlider cardDetailSlider;
    private CenterTab currentTab = CenterTab.Deck;
    private Character? shownCharacter;
    private bool isSetup;

    private void Awake()
    {
        Setup();
    }

    // 도감을 열 때마다 덱 탭부터 표시
    private void OnEnable()
    {
        Setup();
        SetTab(CenterTab.Deck);
    }

    private void OnDisable()
    {
        HideCardDetailPanel();
    }

    private void OnDestroy()
    {
        if (starterCardPanel != null)
        {
            starterCardPanel.CardClicked -= OpenCardDetailPanel;
        }
    }

    public void ShowCharacterDeckInfo(int characterId)
    {
        Character character = (Character)characterId;
        shownCharacter = character;
        ShowStarterCards(character);
        ShowSavedDecks(character);
        RefreshIllustration();
    }

    private void ShowStarterCards(Character character)
    {
        DictionaryStarterCardPanel panel = ResolveStarterCardPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 기본카드 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowCharacterCards(character);
    }

    private void ShowSavedDecks(Character character)
    {
        DictionarySavedDeckPanel panel = ResolveSavedDeckPanel();
        if (panel == null)
        {
            Debug.LogWarning("[RankingModePanel] 저장덱 표시 패널을 찾을 수 없습니다");
            return;
        }

        panel.ShowSavedDecks(character);
    }

    private DictionaryStarterCardPanel ResolveStarterCardPanel()
    {
        return starterCardPanel;
    }

    private DictionarySavedDeckPanel ResolveSavedDeckPanel()
    {
        return savedDeckPanel;
    }

    private void Setup()
    {
        if (isSetup)
        {
            return;
        }

        isSetup = true;
        SetupTabs();
        SetupCardDetail();
    }

    // 탭 버튼은 저장덱 패널과 같은 부모로 옮겨, 선택된 탭 버튼만 패널 앞에 그림
    private void SetupTabs()
    {
        Transform deckPanel = savedDeckPanel != null ? savedDeckPanel.transform : null;
        if (deckPanel == null)
        {
            return;
        }

        if (deckTabButton == null) deckTabButton = FindChildButton(deckPanel, "DeckBtn");
        if (illustrationTabButton == null) illustrationTabButton = FindChildButton(deckPanel, "IllustBtn");

        foreach (Button tabButton in new[] { deckTabButton, illustrationTabButton })
        {
            if (tabButton != null && tabButton.transform.parent != deckPanel.parent)
            {
                tabButton.transform.SetParent(deckPanel.parent, true);
            }
        }

        if (deckTabButton != null)
        {
            deckTabButton.onClick.RemoveListener(OnClickDeckTab);
            deckTabButton.onClick.AddListener(OnClickDeckTab);
        }

        if (illustrationTabButton != null)
        {
            illustrationTabButton.onClick.RemoveListener(OnClickIllustrationTab);
            illustrationTabButton.onClick.AddListener(OnClickIllustrationTab);
        }

        if (illustrationImage == null)
        {
            GameObject illustrationObject = FindChildObject(deckPanel, "CharacterIllust");
            illustrationImage = illustrationObject != null ? illustrationObject.GetComponent<Image>() : null;
        }

        if (illustrationImage == null)
        {
            Debug.LogWarning("[DictionaryPanelController] 일러스트 이미지(CharacterIllust)를 찾지 못해 일러스트 탭에 그림을 표시하지 않습니다", this);
        }
    }

    private void OnClickDeckTab()
    {
        if (currentTab == CenterTab.Deck)
        {
            return;
        }

        SetTab(CenterTab.Deck);
        SettingsManager.PlayPanelToggleSound();
    }

    private void OnClickIllustrationTab()
    {
        if (currentTab == CenterTab.Illustration)
        {
            return;
        }

        SetTab(CenterTab.Illustration);
        SettingsManager.PlayPanelToggleSound();
    }

    private void SetTab(CenterTab tab)
    {
        currentTab = tab;
        bool showDeck = tab == CenterTab.Deck;
        if (savedDeckPanel != null)
        {
            savedDeckPanel.SetDeckPanelsVisible(showDeck);
        }

        if (illustrationImage != null)
        {
            illustrationImage.gameObject.SetActive(!showDeck);
        }

        RefreshIllustration();
        PlaceTabButton(deckTabButton, showDeck);
        PlaceTabButton(illustrationTabButton, !showDeck);
    }

    // 선택된 탭 버튼은 저장덱 패널 바로 앞, 나머지는 바로 뒤
    private void PlaceTabButton(Button tabButton, bool front)
    {
        Transform deckPanel = savedDeckPanel != null ? savedDeckPanel.transform : null;
        if (tabButton == null || deckPanel == null || tabButton.transform.parent != deckPanel.parent)
        {
            return;
        }

        Transform buttonTransform = tabButton.transform;
        int deckIndex = deckPanel.GetSiblingIndex();
        bool isBeforeDeck = buttonTransform.GetSiblingIndex() < deckIndex;
        int targetIndex = front
            ? (isBeforeDeck ? deckIndex : deckIndex + 1)
            : (isBeforeDeck ? deckIndex - 1 : deckIndex);
        buttonTransform.SetSiblingIndex(targetIndex);
    }

    private void RefreshIllustration()
    {
        if (illustrationImage == null || currentTab != CenterTab.Illustration)
        {
            return;
        }

        Sprite sprite = shownCharacter.HasValue
            ? CharacterStandingButtonImageUtility.LoadStandingSprite(shownCharacter.Value)
            : null;
        illustrationImage.sprite = sprite;
        illustrationImage.enabled = sprite != null;
        if (sprite == null)
        {
            return;
        }

        // 영역의 상하 높이만 채우고 너비는 원본 이미지 비율로 맞춤
        illustrationImage.preserveAspect = true;
        AspectRatioFitter fitter = illustrationImage.GetComponent<AspectRatioFitter>();
        if (fitter == null)
        {
            fitter = illustrationImage.gameObject.AddComponent<AspectRatioFitter>();
        }
        fitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        fitter.aspectRatio = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
    }

    // 오른쪽 구역 카드를 누르면 기본형 · 강화형 슬라이드와 효과 툴팁 표시 (전투 카드 확인 패널과 같은 기능)
    private void SetupCardDetail()
    {
        if (cardDetailPanelRoot == null)
        {
            cardDetailPanelRoot = FindChildObject(transform, "DetailPanel");
        }

        if (cardDetailCardPrefab == null && starterCardPanel != null)
        {
            cardDetailCardPrefab = starterCardPanel.CardPrefab;
        }

        if (starterCardPanel != null)
        {
            starterCardPanel.CardClicked -= OpenCardDetailPanel;
            starterCardPanel.CardClicked += OpenCardDetailPanel;
        }

        if (cardDetailPanelRoot == null)
        {
            return;
        }

        // 카드 확인 패널은 자체 배경이 전체 화면을 덮으므로 차단막 없이 등록 (ESC로 닫힘)
        UIModalPanel detailModal = UIModalPanel.Ensure(cardDetailPanelRoot, new UIModalPanel.Options {
            useDimmer = false,
            close = CloseCardDetailPanel
        });
        cardDetailSlider = cardDetailPanelRoot.GetComponent<CardDetailSlider>();
        if (cardDetailSlider == null)
        {
            cardDetailSlider = cardDetailPanelRoot.AddComponent<CardDetailSlider>();
        }
        cardDetailSlider.Configure(cardDetailCardPrefab, cardDetailPanelRoot.transform, detailModal);

        // 전투 씬에서 복사한 닫기 버튼은 다른 씬 대상의 OnClick이 끊겨 있으므로 코드로 연결
        foreach (Button button in cardDetailPanelRoot.GetComponentsInChildren<Button>(true))
        {
            button.onClick.RemoveListener(CloseCardDetailPanel);
            button.onClick.AddListener(CloseCardDetailPanel);
        }

        cardDetailPanelRoot.SetActive(false);
    }

    private void OpenCardDetailPanel(Card card)
    {
        if (card == null || cardDetailPanelRoot == null || cardDetailSlider == null)
        {
            return;
        }

        cardDetailPanelRoot.SetActive(true);
        SettingsManager.PlayPanelToggleSound();
        cardDetailSlider.Show(card);
    }

    public void CloseCardDetailPanel()
    {
        bool wasVisible = cardDetailPanelRoot != null && cardDetailPanelRoot.activeSelf;
        HideCardDetailPanel();
        if (wasVisible)
        {
            SettingsManager.PlayPanelToggleSound();
        }
    }

    private void HideCardDetailPanel()
    {
        if (cardDetailSlider != null)
        {
            cardDetailSlider.HideTooltips();
        }

        if (cardDetailPanelRoot != null)
        {
            cardDetailPanelRoot.SetActive(false);
        }
    }

    private static Button FindChildButton(Transform root, string objectName)
    {
        GameObject found = FindChildObject(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private static GameObject FindChildObject(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == objectName)
            {
                return child.gameObject;
            }
        }
        return null;
    }
}
