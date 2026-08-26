using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AspectCoverImage : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private float fallbackAspect = 16f / 9f;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private Vector2 lastParentSize;
    private float lastAspect;

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

        float aspect = GetImageAspect();
        Vector2 parentSize = parentRectTransform.rect.size;
        if (parentSize == lastParentSize && Mathf.Approximately(aspect, lastAspect)) {
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
        fallbackAspect = Mathf.Max(0.01f, fallbackAspect);
        CacheComponents();
        ApplyLayout();
    }

    private void CacheComponents()
    {
        if (rectTransform == null) {
            rectTransform = transform as RectTransform;
        }

        if (image == null) {
            TryGetComponent(out image);
        }

        parentRectTransform = rectTransform == null ? null : rectTransform.parent as RectTransform;
    }

    private float GetImageAspect()
    {
        if (image != null && image.sprite != null && image.sprite.rect.height > 0f) {
            return image.sprite.rect.width / image.sprite.rect.height;
        }

        return fallbackAspect;
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

        float aspect = GetImageAspect();
        float width = parentSize.x;
        float height = parentSize.y;
        float parentAspect = width / height;

        if (parentAspect > aspect) {
            height = width / aspect;
        } else {
            width = height * aspect;
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(width, height);

        if (image != null) {
            image.preserveAspect = false;
        }

        lastParentSize = parentSize;
        lastAspect = aspect;
    }
}
