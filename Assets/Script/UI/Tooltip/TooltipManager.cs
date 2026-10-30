using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 버프 · 의도 툴팁 패널 하나를 관리합니다
/// </summary>
// 패널은 화면 전체를 덮는 캔버스 아래 마지막 순서에 배치
// 이 컴포넌트는 항상 활성화된 오브젝트에 두고 패널만 켜고 끔
// 크기는 문구에 맞춰 코드에서 계산하므로 패널에 ContentSizeFitter · LayoutGroup이 없어도 됨
// 로비에서 생성되어 씬이 바뀌어도 유지 (자체 루트 캔버스, 로비 · 전투 공용), 같은 오브젝트가 또 생기면 나중 것을 제거
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [Header("툴팁 패널")]
    [Tooltip("배경 이미지가 있는 패널 (피벗은 좌측 상단으로 자동 설정)")]
    [SerializeField] private RectTransform panel;
    [Tooltip("패널 자식 텍스트 (리치 텍스트 사용)")]
    [SerializeField] private TextMeshProUGUI text;

    [Header("크기")]
    [Tooltip("텍스트 최대 너비, 넘으면 줄바꿈")]
    [SerializeField] private float maxTextWidth = 360f;
    [Tooltip("툴팁 패널 최소 크기 (여백 포함), 문구가 짧아도 이 크기보다 작아지지 않음")]
    [SerializeField] private Vector2 minPanelSize = new Vector2(240f, 60f);
    [Tooltip("텍스트 바깥 여백 (x: 좌우, y: 상하)")]
    [SerializeField] private Vector2 padding = new Vector2(12f, 8f);
    [Tooltip("화면 가장자리와 툴팁 사이 최소 간격")]
    [SerializeField] private float screenMargin = 8f;

    [Header("강조 색상")]
    [SerializeField] private TooltipPalette palette = new TooltipPalette();

    private readonly Vector3[] anchorCorners = new Vector3[4];
    private TooltipTrigger currentTrigger;
    private Canvas anchorCanvas;
    private string shownText;
    private bool hasLoggedMissingReference;

    // 포맷터가 사용하는 강조 색상, 툴팁이 없는 씬에서는 기본값
    public static TooltipColors Colors => Instance != null && Instance.palette != null
        ? Instance.palette.ToColors()
        : TooltipColors.Default;

    public static void Show(TooltipTrigger trigger)
    {
        // 패널이 열려 있으면 전투 화면 툴팁(버프 · 의도) 표시 안 함 (몬스터 직접 선택 중은 허용)
        if (!UIModalStack.AllowsBattleTooltips)
        {
            return;
        }

        if (Instance != null && trigger != null)
        {
            Instance.ShowInternal(trigger);
        }
    }

    public static void Hide(TooltipTrigger trigger)
    {
        // 다른 대상으로 이미 넘어간 경우 이전 대상의 이탈 이벤트는 무시
        if (Instance != null && Instance.currentTrigger == trigger)
        {
            Instance.HideInternal();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogError("[TooltipManager] 씬이 바뀌어도 유지되도록 루트 오브젝트에 배치해야 합니다");
        }

        // 툴팁은 모든 화면 위에 그림
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.isRootCanvas)
        {
            canvas.sortingOrder = UICanvasOrder.Tooltip;
        }

        if (panel != null && panel.gameObject == gameObject)
        {
            Debug.LogError("[TooltipManager] 패널을 끄면 이 컴포넌트도 멈추므로 패널과 다른 오브젝트에 배치해야 합니다");
        }

        PreparePanel();
        HideInternal();
    }

    private void OnEnable()
    {
        UIModalStack.Changed += HandleModalChanged;
    }

    private void OnDisable()
    {
        UIModalStack.Changed -= HandleModalChanged;
        HideInternal();
    }

    // 툴팁을 띄운 채 패널이 열리면 닫음
    private void HandleModalChanged()
    {
        if (!UIModalStack.AllowsBattleTooltips)
        {
            HideInternal();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void LateUpdate()
    {
        if (currentTrigger == null)
        {
            // 씬이 바뀌어 대상이 사라지면 패널도 닫음
            if (panel != null && panel.gameObject.activeSelf)
            {
                HideInternal();
            }
            return;
        }

        if (!currentTrigger.isActiveAndEnabled)
        {
            HideInternal();
            return;
        }

        // 카드를 끌어 대상 위에 올릴 때 툴팁이 가리지 않도록 숨기고, 놓은 뒤 아직 호버 중이면 다시 표시
        if (IsCardDragging())
        {
            SetPanelVisible(false);
            shownText = null;
            return;
        }

        // 표시 중에도 매 프레임 문구를 다시 받아 수치 변화를 반영
        string nextText = currentTrigger.GetText();
        if (string.IsNullOrWhiteSpace(nextText))
        {
            SetPanelVisible(false);
            shownText = null;
            return;
        }

        // 비활성 상태에서는 TMP 크기 계산이 맞지 않을 수 있어 먼저 켬 (같은 프레임에 배치하므로 깜빡이지 않음)
        SetPanelVisible(true);
        if (nextText != shownText)
        {
            ApplyText(nextText);
        }

        Reposition();
    }

    private void ShowInternal(TooltipTrigger trigger)
    {
        if (panel == null || text == null)
        {
            // 호버할 때마다 반복 출력되지 않도록 한 번만 기록
            if (!hasLoggedMissingReference)
            {
                hasLoggedMissingReference = true;
                Debug.LogError("[TooltipManager] 툴팁 패널 또는 텍스트가 인스펙터에 연결되지 않았습니다");
            }

            return;
        }

        currentTrigger = trigger;
        anchorCanvas = null;
        shownText = null;
        panel.SetAsLastSibling();
        // 호버한 프레임에 바로 표시
        LateUpdate();
    }

    private void HideInternal()
    {
        currentTrigger = null;
        anchorCanvas = null;
        shownText = null;
        SetPanelVisible(false);
    }

    private void PreparePanel()
    {
        if (panel == null || text == null)
        {
            return;
        }

        panel.pivot = new Vector2(0f, 1f);

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        text.richText = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;

        // 툴팁이 아이콘을 덮어도 호버가 끊기지 않도록 레이캐스트를 받지 않음
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    // 드래그 플래그가 남아 있어도 버튼을 놓았다면 드래그 중이 아님
    private static bool IsCardDragging()
    {
        return CardInteractionHandler.IsAnyCardDragging
            && Mouse.current != null
            && Mouse.current.leftButton.isPressed;
    }

    private void SetPanelVisible(bool visible)
    {
        if (panel != null && panel.gameObject.activeSelf != visible)
        {
            panel.gameObject.SetActive(visible);
        }
    }

    private void ApplyText(string value)
    {
        shownText = value;
        FitPanel(panel, text, value);
    }

    /// <summary>
    /// 전투 툴팁과 같은 모양의 패널 복제본 (카드 확인 패널처럼 여러 개를 나열할 때 사용, 비활성 상태로 반환)
    /// </summary>
    public RectTransform CreatePanelCopy(Transform parent, out TextMeshProUGUI copyText)
    {
        copyText = null;
        if (panel == null || text == null || parent == null)
        {
            return null;
        }

        // 패널에 이 컴포넌트가 함께 있으면 복제본이 Awake에서 Instance를 가져가므로
        // 원본을 잠시 끈 상태로 복제해 Awake를 막고 복제본의 TooltipManager는 바로 제거
        bool wasActive = panel.gameObject.activeSelf;
        panel.gameObject.SetActive(false);
        GameObject copy = Instantiate(panel.gameObject, parent, false);
        panel.gameObject.SetActive(wasActive);
        foreach (TooltipManager copiedView in copy.GetComponentsInChildren<TooltipManager>(true))
        {
            DestroyImmediate(copiedView);
        }
        copy.name = panel.name + "_Copy";
        RectTransform copyRect = (RectTransform)copy.transform;
        copyRect.pivot = new Vector2(0f, 1f);
        copyRect.anchorMin = new Vector2(0.5f, 0.5f);
        copyRect.anchorMax = new Vector2(0.5f, 0.5f);
        copyText = copy.GetComponentInChildren<TextMeshProUGUI>(true);
        return copyRect;
    }

    // 문구에 맞춰 패널 · 텍스트 크기 계산 (전투 툴팁과 같은 최대 너비 · 최소 크기 · 여백)
    public void FitPanel(RectTransform targetPanel, TextMeshProUGUI targetText, string value)
    {
        if (targetPanel == null || targetText == null)
        {
            return;
        }

        targetText.text = value;

        // 한 줄 너비가 최대 너비보다 작으면 그 너비에 맞추고, 크면 최대 너비에서 줄바꿈
        float limit = Mathf.Max(1f, maxTextWidth);
        Vector2 natural = targetText.GetPreferredValues(value, Mathf.Infinity, Mathf.Infinity);
        float width = Mathf.Min(Mathf.Ceil(natural.x) + 1f, limit);
        float height = Mathf.Ceil(targetText.GetPreferredValues(value, width, Mathf.Infinity).y);

        // 최소 크기를 적용한 뒤 텍스트는 늘어난 너비 안에서 좌측 상단부터 배치 (줄바꿈 위치는 그대로)
        float panelWidth = Mathf.Max(width + padding.x * 2f, minPanelSize.x);
        float panelHeight = Mathf.Max(height + padding.y * 2f, minPanelSize.y);

        RectTransform textRect = targetText.rectTransform;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(width, panelWidth - padding.x * 2f));
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        textRect.anchoredPosition = new Vector2(padding.x, -padding.y);

        targetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
        targetPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
    }

    public float ScreenMargin => screenMargin;

    private void Reposition()
    {
        RectTransform parent = panel.parent as RectTransform;
        RectTransform anchorRect = currentTrigger.Anchor;
        if (parent == null || anchorRect == null)
        {
            return;
        }

        if (anchorCanvas == null)
        {
            anchorCanvas = anchorRect.GetComponentInParent<Canvas>();
        }

        // 기준 영역과 툴팁이 다른 캔버스에 있어도 화면 좌표를 거쳐 같은 좌표계로 변환
        Camera anchorCamera = ResolveCanvasCamera(anchorCanvas);
        Camera panelCamera = ResolveCanvasCamera(panel.GetComponentInParent<Canvas>());
        anchorRect.GetWorldCorners(anchorCorners);
        Vector2 min = ToParentLocal(parent, anchorCorners[0], anchorCamera, panelCamera);
        Vector2 max = ToParentLocal(parent, anchorCorners[2], anchorCamera, panelCamera);

        Rect bounds = parent.rect;
        Vector2 size = panel.rect.size;
        (float x, float y) topLeft = TooltipLayout.ResolveTopLeft(
            currentTrigger.Placement,
            new TooltipRect(min.x, min.y, max.x, max.y),
            size.x,
            size.y,
            currentTrigger.Spacing,
            new TooltipRect(bounds.xMin, bounds.yMin, bounds.xMax, bounds.yMax),
            screenMargin);

        panel.localPosition = new Vector3(topLeft.x, topLeft.y, 0f);
    }

    private static Vector2 ToParentLocal(RectTransform parent, Vector3 worldPoint, Camera sourceCamera, Camera targetCamera)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, targetCamera, out Vector2 localPoint);
        return localPoint;
    }

    private static Camera ResolveCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
        {
            return null;
        }

        Canvas rootCanvas = canvas.rootCanvas;
        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }
}
