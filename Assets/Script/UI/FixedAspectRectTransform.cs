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

    private void Awake()
    {
        CacheComponents();
        ApplyLayout();
    }

    private void OnEnable()
    {
        CacheComponents();
        ApplyLayout();
    }

    private void Update()
    {
        if (parentRectTransform == null) {
            CacheComponents();
        }

        if (parentRectTransform == null) {
            return;
        }

        Vector2 parentSize = parentRectTransform.rect.size;
        if (parentSize == lastParentSize && Mathf.Approximately(targetAspect, lastTargetAspect)) {
            return;
        }

        ApplyLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout();
    }

    private void OnValidate()
    {
        targetAspect = Mathf.Max(0.01f, targetAspect);
        CacheComponents();
        ApplyLayout();
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

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(width, height);

        lastParentSize = parentSize;
        lastTargetAspect = targetAspect;
    }
}
