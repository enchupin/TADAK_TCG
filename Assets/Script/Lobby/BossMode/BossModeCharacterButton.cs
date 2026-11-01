using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 모드 캐릭터 버튼 (훈련 모드 CharacterSelectButton 복제 프리팹에 부착)
/// 덱을 고르면 버튼 오른쪽 확장 영역에 덱 카드 8칸(빈 칸 포함)을 작은 카드로 표시
/// </summary>
public class BossModeCharacterButton : MonoBehaviour
{
    private const int SlotCount = CharacterDeckSave.MaxCardCount;

    [Header("선택 표시")]
    [Tooltip("선택 색을 바꿀 이미지 (비우면 같은 오브젝트의 Image)")]
    [SerializeField] private Image background;
    [SerializeField] private Color selectedColor = Color.cyan;

    [Header("확장 영역 (버튼 오른쪽, 덱을 고르면 표시)")]
    [SerializeField] private RectTransform expandArea;
    [Tooltip("카드를 배치할 영역 (비우면 확장 영역), 좌측 상단부터 채움")]
    [SerializeField] private RectTransform cardRoot;
    [Tooltip("확장 시 다음 버튼을 밀어낼 폭 (0이면 확장 영역 폭)")]
    [SerializeField] private float expandedWidth;

    [Header("카드 배치")]
    [SerializeField] private GameObject cardPrefab;
    [Tooltip("빈 칸 표시 프리팹 (비우면 빈 칸은 비워 둠)")]
    [SerializeField] private GameObject emptySlotPrefab;
    [SerializeField] private int columns = 4;
    [Tooltip("칸 크기, 카드는 비율을 유지해 칸에 맞춤")]
    [SerializeField] private Vector2 slotSize = new Vector2(90f, 126f);
    [SerializeField] private Vector2 slotSpacing = new Vector2(8f, 8f);
    [Tooltip("확장 영역 안쪽 여백 (좌, 상)")]
    [SerializeField] private Vector2 padding = new Vector2(8f, 8f);

    private readonly List<GameObject> createdSlots = new List<GameObject>();
    private Action<Character> clicked;
    private Button button;
    private Color normalColor = Color.white;
    private bool hasNormalColor;
    private string shownDeckId;

    public Character Character { get; private set; }
    public bool IsExpanded => expandArea != null && expandArea.gameObject.activeSelf;
    public RectTransform RectTransform => (RectTransform)transform;

    // 다음 버튼을 밀어낼 추가 폭
    public float ExtraWidth
    {
        get
        {
            if (!IsExpanded)
            {
                return 0f;
            }

            return expandedWidth > 0f ? expandedWidth : expandArea.rect.width;
        }
    }

    public void Initialize(CharacterData characterData, Action<Character> onClicked)
    {
        Character = (Character)characterData.characterId;
        clicked = onClicked;
        CharacterButtonVisuals.Apply(gameObject, characterData);

        if (background == null)
        {
            TryGetComponent(out background);
        }

        if (background != null && !hasNormalColor)
        {
            normalColor = background.color;
            hasNormalColor = true;
        }

        // 훈련 모드 선택 기능이 남아 있으면 같은 클릭으로 훈련 선택 목록까지 바뀜
        if (GetComponent<SelectedButtonControl>() != null)
        {
            Debug.LogWarning("[BossModeCharacterButton] 복제 프리팹에서 SelectedButtonControl과 그 OnClick 연결을 제거해야 합니다", this);
        }

        if (button == null && TryGetComponent(out button))
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        SetState(false, null);
    }

    private void OnClick()
    {
        SettingsManager.PlayPanelToggleSound();
        clicked?.Invoke(Character);
    }

    /// <summary>
    /// 선택 상태 · 고른 덱 표시 (deck이 null이면 확장 영역을 접음)
    /// </summary>
    public void SetState(bool selected, CharacterDeckSave deck)
    {
        if (background != null)
        {
            background.color = selected ? selectedColor : normalColor;
        }

        bool expand = selected && deck != null;
        if (expandArea != null && expandArea.gameObject.activeSelf != expand)
        {
            expandArea.gameObject.SetActive(expand);
        }

        string deckKey = expand ? DeckKey(deck) : null;
        if (deckKey == shownDeckId)
        {
            return;
        }

        shownDeckId = deckKey;
        ClearSlots();
        if (expand)
        {
            BuildSlots(deck);
        }
    }

    // 같은 덱이라도 카드가 바뀌면 다시 그림
    private static string DeckKey(CharacterDeckSave deck)
    {
        return deck.deckId + "|" + (deck.cardIds != null ? string.Join(",", deck.cardIds) : string.Empty);
    }

    private void BuildSlots(CharacterDeckSave deck)
    {
        RectTransform root = cardRoot != null ? cardRoot : expandArea;
        if (root == null)
        {
            Debug.LogWarning("[BossModeCharacterButton] 확장 영역이 연결되지 않았습니다", this);
            return;
        }

        List<int> cardIds = deck.cardIds != null ? new List<int>(deck.cardIds) : new List<int>();
        cardIds.Sort();
        for (int i = 0; i < SlotCount; i++)
        {
            Card card = i < cardIds.Count ? CardManager.GetCardAsCard(cardIds[i]) : null;
            if (i < cardIds.Count && card == null)
            {
                Debug.LogWarning($"[BossModeCharacterButton] 카드 데이터를 찾을 수 없습니다: {cardIds[i]}");
            }

            GameObject prefab = card != null ? cardPrefab : emptySlotPrefab;
            if (prefab == null)
            {
                continue;
            }

            GameObject slot = Instantiate(prefab, root, false);
            slot.name = card != null ? $"BossModeDeckCard_{card.cardId}" : $"BossModeEmptySlot_{i}";
            DisableRaycasts(slot);
            PlaceInSlot((RectTransform)slot.transform, i);
            if (card != null)
            {
                SetupCard(slot, card);
            }

            createdSlots.Add(slot);
        }
    }

    // 좌측 상단부터 columns 개씩 채우고, 비율을 유지한 채 칸 크기에 맞춤
    private void PlaceInSlot(RectTransform rect, int index)
    {
        int safeColumns = Mathf.Max(1, columns);
        int column = index % safeColumns;
        int row = index / safeColumns;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(
            padding.x + column * (slotSize.x + slotSpacing.x) + slotSize.x * 0.5f,
            -(padding.y + row * (slotSize.y + slotSpacing.y) + slotSize.y * 0.5f));

        Vector2 size = rect.rect.size;
        float scale = size.x > 0f && size.y > 0f ? Mathf.Min(slotSize.x / size.x, slotSize.y / size.y) : 1f;
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    private static void SetupCard(GameObject cardObject, Card card)
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
        }
    }

    // 카드 확인 기능 없음: 작은 카드가 버튼 클릭을 가로채지 않게 함
    private static void DisableRaycasts(GameObject target)
    {
        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        foreach (UIHoverEffect hoverEffect in target.GetComponentsInChildren<UIHoverEffect>(true))
        {
            hoverEffect.isHoverable = false;
        }
    }

    private void ClearSlots()
    {
        foreach (GameObject slot in createdSlots)
        {
            if (slot != null)
            {
                slot.SetActive(false);
                Destroy(slot);
            }
        }

        createdSlots.Clear();
    }
}
