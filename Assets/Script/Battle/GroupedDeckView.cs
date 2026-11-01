using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전체 덱(상단 바 덱 버튼) 화면: 파티 캐릭터별 머리줄 + 카드 격자
/// 덱 패널 스크롤의 별도 Content에 붙이고, 전체 덱을 볼 때만 스크롤 Content를 이쪽으로 바꿔 사용
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class GroupedDeckView : MonoBehaviour
{
    [Header("프리팹")]
    [SerializeField] private DeckGroupHeader headerPrefab;
    [SerializeField] private GameObject cardPrefab;

    [Header("카드 배치")]
    [Tooltip("카드 배율 (카드 확인 화면 크기 = 1)")]
    [SerializeField] private float cardScale = 1f;
    [Tooltip("카드 원래 크기 (카드 프리팹 크기)")]
    [SerializeField] private Vector2 cardSize = new Vector2(200f, 280f);
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 cardSpacing = new Vector2(16f, 16f);

    [Header("묶음 배치")]
    [SerializeField] private RectOffset padding = new RectOffset(16, 16, 16, 16);
    [Tooltip("머리줄과 카드 사이 · 묶음 사이 간격")]
    [SerializeField] private float sectionSpacing = 16f;

    // 실행 중 스크립트 재컴파일 등으로 비어 있을 수 있어 사용할 때 생성
    private List<GameObject> createdObjects;
    private List<GameObject> CreatedObjects => createdObjects ??= new List<GameObject>();
    private bool isLayoutReady;

    public RectTransform Content => (RectTransform)transform;

    /// <summary>
    /// 카드를 파티 순서대로 묶어 표시, 파티에 없는 캐릭터 카드는 마지막 기타 묶음
    /// </summary>
    public void Show(List<Card> cards, IReadOnlyList<Character> party, Action<CardController> onCardClicked)
    {
        EnsureLayout();
        Clear();
        if (cards == null)
        {
            return;
        }

        List<Character> order = party != null ? new List<Character>(party) : new List<Character>();
        order.Sort();
        Dictionary<Character, List<Card>> groups = new Dictionary<Character, List<Card>>();
        List<Card> others = new List<Card>();
        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            if (!order.Contains(card.character))
            {
                others.Add(card);
                continue;
            }

            if (!groups.TryGetValue(card.character, out List<Card> list))
            {
                list = new List<Card>();
                groups[card.character] = list;
            }
            list.Add(card);
        }

        foreach (Character character in order)
        {
            if (groups.TryGetValue(character, out List<Card> list))
            {
                CreateHeader()?.BindCharacter(character);
                CreateGrid(list, onCardClicked);
            }
        }

        if (others.Count > 0)
        {
            CreateHeader()?.BindOther();
            CreateGrid(others, onCardClicked);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
    }

    public void Clear()
    {
        if (createdObjects == null)
        {
            return;
        }

        foreach (GameObject created in createdObjects)
        {
            if (created != null)
            {
                created.SetActive(false);
                Destroy(created);
            }
        }

        createdObjects.Clear();
    }

    // 묶음을 위에서 아래로 쌓고 높이는 내용에 맞춤 (스크롤용)
    private void EnsureLayout()
    {
        if (isLayoutReady)
        {
            return;
        }

        isLayoutReady = true;
        VerticalLayoutGroup vertical = GetComponent<VerticalLayoutGroup>();
        if (vertical == null)
        {
            vertical = gameObject.AddComponent<VerticalLayoutGroup>();
        }

        vertical.padding = padding;
        vertical.spacing = sectionSpacing;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private DeckGroupHeader CreateHeader()
    {
        if (headerPrefab == null)
        {
            Debug.LogWarning("[GroupedDeckView] 머리줄 프리팹이 연결되지 않았습니다", this);
            return null;
        }

        DeckGroupHeader header = Instantiate(headerPrefab, Content, false);
        // 세로 배치에서 프리팹 높이를 유지
        LayoutElement element = header.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = header.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = ((RectTransform)headerPrefab.transform).rect.height;
        }

        CreatedObjects.Add(header.gameObject);
        return header;
    }

    private void CreateGrid(List<Card> cards, Action<CardController> onCardClicked)
    {
        GameObject gridObject = new GameObject("CardGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.layer = gameObject.layer;
        gridObject.transform.SetParent(Content, false);
        CreatedObjects.Add(gridObject);

        Vector2 cellSize = cardSize * cardScale;
        GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = cardSpacing;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);

        foreach (Card card in cards)
        {
            CreateCard(gridObject.transform, card, onCardClicked);
        }
    }

    // 격자 칸은 빈 슬롯, 카드는 슬롯 가운데에 배율만 적용 (격자가 카드 크기를 바꾸지 않게 함)
    private void CreateCard(Transform grid, Card card, Action<CardController> onCardClicked)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("[GroupedDeckView] 카드 프리팹이 연결되지 않았습니다", this);
            return;
        }

        GameObject slot = new GameObject("CardSlot", typeof(RectTransform));
        slot.layer = gameObject.layer;
        slot.transform.SetParent(grid, false);

        GameObject cardObject = Instantiate(cardPrefab, slot.transform, false);
        RectTransform cardRect = (RectTransform)cardObject.transform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one * cardScale;
        UIHoverEffect hoverEffect = cardObject.GetComponent<UIHoverEffect>();
        hoverEffect?.CaptureCurrentScaleAsOriginal();

        CardController controller = cardObject.GetComponent<CardController>();
        if (controller == null)
        {
            Debug.LogWarning("[GroupedDeckView] 카드 프리팹에서 CardController를 찾을 수 없습니다", cardObject);
            return;
        }

        controller.Initialize(card);
        controller.useInteractionHandler = false;
        if (onCardClicked != null)
        {
            CardSelectionClickHandler clickHandler = cardObject.GetComponent<CardSelectionClickHandler>();
            if (clickHandler == null)
            {
                clickHandler = cardObject.AddComponent<CardSelectionClickHandler>();
            }
            clickHandler.Bind(controller, () => onCardClicked(controller));
        }
    }
}
