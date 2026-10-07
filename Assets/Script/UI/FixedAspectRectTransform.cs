using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FixedAspectRectTransform : MonoBehaviour
{
    [SerializeField] private float targetAspect = 16f / 9f;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Vector2 lastParentSize;
    private float lastTargetAspect;
    private bool layoutDirty = true;
    private bool isApplyingLayout;

    private void Awake()
    {
        CacheComponents();
        layoutDirty = true;
    }

    private void OnEnable()
    {
        CacheComponents();
        layoutDirty = true;
    }

    private void Update()
    {
        if (layoutDirty || parentRectTransform == null) {
            CacheComponents();
        }

        if (parentRectTransform == null) {
            return;
        }

        Vector2 parentSize = parentRectTransform.rect.size;
        if (!layoutDirty && parentSize == lastParentSize && Mathf.Approximately(targetAspect, lastTargetAspect)) {
            return;
        }

        layoutDirty = false;
        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isApplyingLayout) {
            layoutDirty = true;
        }
    }

    private void OnTransformParentChanged()
    {
        layoutDirty = true;
    }

    private void OnValidate()
    {
        targetAspect = Mathf.Max(0.01f, targetAspect);
        // 검증 중에는 레이아웃을 변경하지 않고 다음 업데이트에서 적용
        layoutDirty = true;
    }

    private void CacheComponents()
    {
        if (rectTransform == null) {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null) {
            parentRectTransform = null;
            return;
        }

        parentRectTransform = rectTransform.parent as RectTransform;
    }

    private void ApplyLayout()
    {
        if (rectTransform == null || parentRectTransform == null) {
            return;
        }

        Vector2 parentSize = parentRectTransform.rect.size;
        if (parentSize.x <= 0f || parentSize.y <= 0f) {
            return;
        }

        float width = parentSize.x;
        float height = parentSize.y;
        float parentAspect = width / height;

        if (parentAspect > targetAspect) {
            width = height * targetAspect;
        } else {
            height = width / targetAspect;
        }

        isApplyingLayout = true;
        try
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(width, height);
        }
        finally
        {
            isApplyingLayout = false;
        }

        lastParentSize = parentSize;
        lastTargetAspect = targetAspect;
    }
}
