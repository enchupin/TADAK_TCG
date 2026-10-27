using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버프 · 의도 아이콘 하나 (아이콘 이미지 + 우하단 숫자)
/// </summary>
// 프리팹으로 만들 때는 루트에 붙이고 아이콘 Image와 숫자 TMP를 연결
// 프리팹이 없으면 CreateDefault로 실행 중 기본 아이콘을 만들어 사용
[DisallowMultipleComponent]
public class StatusIconView : MonoBehaviour
{
    [Tooltip("아이콘 이미지, 비어 있으면 루트의 Image")]
    [SerializeField] private Image icon;
    [Tooltip("중첩 · 수치 텍스트 (비어 있으면 숫자 생략)")]
    [SerializeField] private TextMeshProUGUI countText;

    public RectTransform RectTransform => transform as RectTransform;

    public void Set(Sprite sprite, string count)
    {
        Image image = ResolveIcon();
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
            image.preserveAspect = true;
            // 툴팁 호버를 받도록 표시 중인 아이콘만 레이캐스트를 켬
            image.raycastTarget = sprite != null;
        }

        SetCount(count);
    }

    public void SetCount(string count)
    {
        if (countText == null)
        {
            return;
        }

        bool hasCount = !string.IsNullOrEmpty(count);
        countText.text = hasCount ? count : string.Empty;
        countText.enabled = hasCount;
        countText.raycastTarget = false;
    }

    public void SetSize(float size)
    {
        RectTransform rect = RectTransform;
        if (rect == null)
        {
            return;
        }

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
    }

    private Image ResolveIcon()
    {
        if (icon == null)
        {
            icon = GetComponent<Image>();
        }

        return icon;
    }

    // 프리팹 없이 쓰는 기본 아이콘: 루트 Image + 우하단 숫자
    public static StatusIconView CreateDefault(Transform parent, string objectName, float size, float fontSize)
    {
        GameObject root = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = parent != null ? parent.gameObject.layer : root.layer;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.sizeDelta = new Vector2(size, size);

        Image image = root.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;

        GameObject countObject = new("Count", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        countObject.layer = root.layer;
        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.SetParent(rootRect, false);
        // 아이콘 우하단 모서리에 붙이고 아이콘 밖으로 조금 나가도 보이게 함
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(2f, -2f);
        countRect.sizeDelta = new Vector2(size, fontSize);

        TextMeshProUGUI text = countObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.BottomRight;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;
        text.enabled = false;

        StatusIconView view = root.AddComponent<StatusIconView>();
        view.icon = image;
        view.countText = text;
        return view;
    }
}
