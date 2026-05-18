using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonCreateManager : MonoBehaviour {
    public enum Scrolltype {
        horizontal,
        vertical
    }

    [Serializable]
    public sealed class ButtonPanelConfig {
        public RectTransform content;
        public RectTransform scrollView;
        public GameObject buttonPrefab;
        [HideInInspector] public Vector3 startPosition;
        [HideInInspector] public float buttonSpacing;
        [HideInInspector] public float trailingPadding;
        [HideInInspector] public float scrollSensitivity;
        [HideInInspector] public Scrolltype scrolltype;
    }

    private const float DefaultScrollViewWidth = 800f;
    private const float DefaultScrollViewHeight = 370f;

    [SerializeField] private ButtonPanelConfig trainingPanelConfig;
    [SerializeField] private ButtonPanelConfig characterBookPanelConfig;

    private readonly HashSet<RectTransform> createdContents = new();
    private CardContainerManager characterBookCardManager;

    private void Start() {
        InitiatePanelCofig();
        StartCoroutine(CreateButtonsAfterLayout());
    }

    private IEnumerator CreateButtonsAfterLayout() {
        yield return null;
        Canvas.ForceUpdateCanvases();
        CreateButtons(trainingPanelConfig);
        CreateButtons(characterBookPanelConfig);
    }

    private void InitiatePanelCofig() {
        trainingPanelConfig ??= new ButtonPanelConfig();
        characterBookPanelConfig ??= new ButtonPanelConfig();

        trainingPanelConfig.startPosition = new Vector3(160f, 0f, 0f);
        trainingPanelConfig.buttonSpacing = 240f;
        trainingPanelConfig.trailingPadding = 80f;
        trainingPanelConfig.scrollSensitivity = 7.5f;
        trainingPanelConfig.scrolltype = Scrolltype.horizontal;

        characterBookPanelConfig.startPosition = new Vector3(186.5f, -60f, 0f);
        characterBookPanelConfig.buttonSpacing = 80f;
        characterBookPanelConfig.trailingPadding = 35f;
        characterBookPanelConfig.scrollSensitivity = 5.5f;
        characterBookPanelConfig.scrolltype = Scrolltype.vertical;
    }


    public void CreateButtons(ButtonPanelConfig buttonPanelConfig) {
        if (!ValidatePanelConfig(buttonPanelConfig)) {
            return;
        }
        SetupScrollView(buttonPanelConfig);
        if (!createdContents.Add(buttonPanelConfig.content)) {
            return;
        }
        List<CharacterData> characters = CharacterManager.GetAllCharacters();
        if (characters == null || characters.Count == 0) {
            Debug.LogWarning("[ButtonCreateManager] 생성할 캐릭터 데이터가 없습니다");
            return;
        }
        characters.Sort(CompareCharacterOrder);
        int createdButtonCount = 0;
        foreach (CharacterData characterData in characters) {
            if (characterData == null)
                continue;

            GameObject createdButton = Instantiate(buttonPanelConfig.buttonPrefab, buttonPanelConfig.content, false);
            createdButton.name = $"CharacterButton_{characterData.characterId}";

            RectTransform buttonRect = createdButton.GetComponent<RectTransform>();
            SetButtonPosition(buttonRect, buttonPanelConfig, createdButtonCount);

            BindCharacterData(createdButton, characterData, ShouldApplyStandingImage(buttonPanelConfig));
            createdButtonCount++;
        }

        UpdateContentSize(createdButtonCount, buttonPanelConfig);
    }

    private static bool ValidatePanelConfig(ButtonPanelConfig buttonPanelConfig) {
        if (buttonPanelConfig == null) {
            return false;
        }
        if (buttonPanelConfig.content == null && buttonPanelConfig.scrollView == null && buttonPanelConfig.buttonPrefab == null) {
            return false;
        }
        if (buttonPanelConfig.content == null) {
            Debug.LogError("[ButtonCreateManager] Content가 연결되지 않았습니다");
            return false;
        }
        if (buttonPanelConfig.scrollView == null) {
            Debug.LogError("[ButtonCreateManager] Scroll View가 연결되지 않았습니다");
            return false;
        }
        if (buttonPanelConfig.buttonPrefab == null) {
            Debug.LogError("[ButtonCreateManager] 버튼 프리팹이 연결되지 않았습니다");
            return false;
        }
        return true;
    }

    private static void SetupScrollView(ButtonPanelConfig buttonPanelConfig) {
        ScrollRect scrollRect = buttonPanelConfig.scrollView.GetComponent<ScrollRect>();
        if (scrollRect == null) {
            Debug.LogWarning("[ButtonCreateManager] ScrollRect 컴포넌트를 찾을 수 없습니다");
            return;
        }

        scrollRect.scrollSensitivity = buttonPanelConfig.scrollSensitivity;

        RectTransform wheelTarget = scrollRect.viewport != null ? scrollRect.viewport : buttonPanelConfig.scrollView;
        ScrollRectWheelRouter wheelRouter = wheelTarget.GetComponent<ScrollRectWheelRouter>();
        if (buttonPanelConfig.scrolltype == Scrolltype.horizontal) {
            if (wheelRouter == null) {
                wheelRouter = wheelTarget.gameObject.AddComponent<ScrollRectWheelRouter>();
            }
            wheelRouter.Bind(scrollRect);
            wheelRouter.enabled = true;
            return;
        }

        if (wheelRouter != null) {
            wheelRouter.enabled = false;
        }
    }

    private static void SetButtonPosition(RectTransform buttonRect, ButtonPanelConfig buttonPanelConfig, int createdButtonCount) {
        if (buttonRect == null) {
            return;
        }
        float x = buttonPanelConfig.startPosition.x;
        float y = buttonPanelConfig.startPosition.y;

        if (buttonPanelConfig.scrolltype == Scrolltype.horizontal) {
            x += buttonPanelConfig.buttonSpacing * createdButtonCount;
        } else {
            y -= buttonPanelConfig.buttonSpacing * createdButtonCount;
        }
        buttonRect.anchoredPosition3D = new Vector3(x, y, buttonPanelConfig.startPosition.z);
        buttonRect.localScale = Vector3.one;
    }

    private static int CompareCharacterOrder(CharacterData left, CharacterData right) {
        if (left == null && right == null) {
            return 0;
        }
        if (left == null) {
            return 1;
        }
        if (right == null) {
            return -1;
        }

        return left.characterId.CompareTo(right.characterId);
    }

    private bool ShouldApplyStandingImage(ButtonPanelConfig buttonPanelConfig) {
        return ReferenceEquals(buttonPanelConfig, trainingPanelConfig);
    }

    private void BindCharacterData(GameObject buttonObject, CharacterData characterData, bool shouldApplyStandingImage) {
        BindCharacterLabel(buttonObject, characterData);
        SetButtonText(buttonObject, characterData.characterName);
        if (!Enum.IsDefined(typeof(Character), characterData.characterId)) {
            Debug.LogWarning($"[ButtonCreateManager] Character enum에 없는 characterId입니다: {characterData.characterId}");
            return;
        }
        Character character = (Character)characterData.characterId;
        if (shouldApplyStandingImage) {
            CharacterStandingButtonImageUtility.Apply(buttonObject, character);
        }
        BindTrainingButton(buttonObject, character);
        BindCharacterBookButton(buttonObject, character);
    }

    private static void BindCharacterLabel(GameObject buttonObject, CharacterData characterData) {
        if (buttonObject == null || characterData == null) {
            return;
        }

        CharacterLocalizedButtonLabel localizedLabel = buttonObject.GetComponent<CharacterLocalizedButtonLabel>();
        if (localizedLabel == null) {
            localizedLabel = buttonObject.AddComponent<CharacterLocalizedButtonLabel>();
        }

        localizedLabel.Bind(characterData.characterId);
    }

    private static void BindTrainingButton(GameObject buttonObject, Character character) {
        SelectedButtonControl selectedButtonControl = buttonObject.GetComponent<SelectedButtonControl>();
        if (selectedButtonControl == null) {
            return;
        }
        selectedButtonControl.Initialize(character);
    }

    private void BindCharacterBookButton(GameObject buttonObject, Character character) {
        CharacterBookButton characterBookButton = buttonObject.GetComponent<CharacterBookButton>();
        if (characterBookButton == null) {
            return;
        }
        CardContainerManager cardManager = GetCharacterBookCardManager();
        if (cardManager == null) {
            Debug.LogError("[ButtonCreateManager] CharacterBook용 CardContainerManager를 찾을 수 없습니다");
            return;
        }

        characterBookButton.Initialize(character, cardManager);
    }

    private CardContainerManager GetCharacterBookCardManager() {
        if (characterBookCardManager != null) {
            return characterBookCardManager;
        }

        characterBookCardManager = FindCardContainerManager();
        return characterBookCardManager;
    }

    private static CardContainerManager FindCardContainerManager() {
        CardContainerManager[] managers = Resources.FindObjectsOfTypeAll<CardContainerManager>();
        foreach (CardContainerManager manager in managers) {
            if (manager != null && manager.gameObject.scene.IsValid() && manager.gameObject.scene.isLoaded) {
                return manager;
            }
        }

        return null;
    }

    private static void SetButtonText(GameObject buttonObject, string characterName) {
        TMP_Text tmpText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null) {
            tmpText.text = characterName;
            return;
        }

        Text legacyText = buttonObject.GetComponentInChildren<Text>(true);
        if (legacyText != null) {
            legacyText.text = characterName;
        }
    }

    private void UpdateContentSize(int buttonCount, ButtonPanelConfig buttonPanelConfig) {
        if (buttonPanelConfig.content == null || buttonPanelConfig.scrollView == null || buttonCount <= 0)
            return;

        Canvas.ForceUpdateCanvases();

        float scrollViewWidth = GetScrollViewWidth(buttonPanelConfig.scrollView);
        float scrollViewHeight = GetScrollViewHeight(buttonPanelConfig.scrollView);
        RectTransform prefabRect = buttonPanelConfig.buttonPrefab.GetComponent<RectTransform>();
        float buttonWidth = prefabRect != null ? prefabRect.rect.width : 0f;
        float buttonHeight = prefabRect != null ? prefabRect.rect.height : 0f;

        if (buttonPanelConfig.scrolltype == Scrolltype.horizontal) {
            float lastButtonRightEdge = buttonPanelConfig.startPosition.x + (buttonPanelConfig.buttonSpacing * (buttonCount - 1)) + (buttonWidth * 0.5f);
            float requiredWidth = Mathf.Max(scrollViewWidth, lastButtonRightEdge + buttonPanelConfig.trailingPadding);
            buttonPanelConfig.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
            buttonPanelConfig.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            return;
        }

        float lastButtonBottomEdge = buttonPanelConfig.startPosition.y - (buttonPanelConfig.buttonSpacing * (buttonCount - 1)) - (buttonHeight * 0.5f);
        float requiredHeight = Mathf.Max(scrollViewHeight, - lastButtonBottomEdge + buttonPanelConfig.trailingPadding);
        buttonPanelConfig.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
        buttonPanelConfig.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
    }

    private static float GetScrollViewWidth(RectTransform scrollView) {
        if (scrollView == null) {
            return DefaultScrollViewWidth;
        }
        float width = scrollView.rect.width;
        return width > 0f ? width : DefaultScrollViewWidth;
    }

    private static float GetScrollViewHeight(RectTransform scrollView) {
        if (scrollView == null) {
            return DefaultScrollViewHeight;
        }
        float height = scrollView.rect.height;
        return height > 0f ? height : DefaultScrollViewHeight;
    }

}
