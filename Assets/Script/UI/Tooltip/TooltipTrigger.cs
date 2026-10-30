using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스를 올리면 기준 영역 옆 고정 위치에 툴팁을 표시합니다
/// </summary>
// 같은 오브젝트에 ITooltipSource가 있으면 자동으로 사용하고, 없으면 SetSource로 연결
// 호버를 받으려면 이 오브젝트나 자식 Graphic의 raycastTarget이 켜져 있어야 함
[DisallowMultipleComponent]
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("툴팁 배치")]
    [Tooltip("툴팁 위치 기준 영역, 비어 있으면 이 오브젝트의 RectTransform")]
    [SerializeField] private RectTransform anchor;
    [SerializeField] private TooltipPlacement placement = TooltipPlacement.Bottom;
    [Tooltip("기준 영역과 툴팁 사이 간격")]
    [SerializeField] private float spacing = 4f;

    private ITooltipSource source;
    private bool hasResolvedSource;

    public RectTransform Anchor => anchor != null ? anchor : transform as RectTransform;
    public TooltipPlacement Placement => placement;
    public float Spacing => spacing;

    public void SetSource(ITooltipSource tooltipSource)
    {
        source = tooltipSource;
        hasResolvedSource = true;
    }

    // 코드에서 붙일 때 배치 기본값 지정 (인스펙터에서 붙인 경우 호출하지 않음)
    public void SetPlacement(TooltipPlacement tooltipPlacement, float tooltipSpacing)
    {
        placement = tooltipPlacement;
        spacing = tooltipSpacing;
    }

    public string GetText()
    {
        if (!hasResolvedSource)
        {
            source = GetComponent<ITooltipSource>();
            hasResolvedSource = true;
        }

        return source?.GetTooltipText();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Show(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Hide(this);
    }

    private void OnDisable()
    {
        TooltipManager.Hide(this);
    }
}
