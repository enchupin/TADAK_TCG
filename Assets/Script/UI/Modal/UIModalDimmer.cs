using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 패널 뒤 전체 화면 어두운 차단막 (클릭해도 동작 없음)
/// 지정한 통과 대상 위에서는 레이캐스트를 통과시켜 뒤쪽 버튼이 눌리게 함
/// </summary>
[RequireComponent(typeof(RectTransform))]
public sealed class UIModalDimmer : MonoBehaviour, ICanvasRaycastFilter
{
    private readonly List<RectTransform> passThroughTargets = new List<RectTransform>();
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Vector2 lastRootSize;

    public static UIModalDimmer Create(Transform panel, Color color)
    {
        Transform parent = panel != null ? panel.parent : null;
        if (parent == null)
        {
            Debug.LogWarning("[UIModalDimmer] 패널의 부모가 없어 차단막을 만들 수 없습니다");
            return null;
        }

        GameObject root = new GameObject(panel.name + "_Dimmer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = panel.gameObject.layer;
        root.transform.SetParent(parent, false);

        Image image = root.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        UIModalDimmer dimmer = root.AddComponent<UIModalDimmer>();
        dimmer.rectTransform = (RectTransform)root.transform;
        return dimmer;
    }

    public void SetPassThrough(List<RectTransform> targets)
    {
        passThroughTargets.Clear();
        if (targets != null)
        {
            passThroughTargets.AddRange(targets);
        }
    }

    public void PlaceBehind(Transform panel)
    {
        int panelIndex = panel.GetSiblingIndex();
        int dimmerIndex = transform.GetSiblingIndex();
        transform.SetSiblingIndex(dimmerIndex < panelIndex ? panelIndex - 1 : panelIndex);
        FitToRootCanvas();
    }

    private void LateUpdate()
    {
        // 해상도가 바뀌면 다시 맞춤
        if (rootCanvas != null && ((RectTransform)rootCanvas.transform).rect.size != lastRootSize)
        {
            FitToRootCanvas();
        }
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        foreach (RectTransform target in passThroughTargets)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                continue;
            }

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, targetCamera))
            {
                return false;
            }
        }
        return true;
    }

    // 부모가 전체 화면이 아니어도 루트 캔버스 전체를 덮도록 크기 · 위치 계산
    private void FitToRootCanvas()
    {
        if (rectTransform == null)
        {
            rectTransform = (RectTransform)transform;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
        RectTransform parent = rectTransform.parent as RectTransform;
        if (rootCanvas == null || parent == null)
        {
            return;
        }

        RectTransform rootRect = (RectTransform)rootCanvas.transform;
        lastRootSize = rootRect.rect.size;

        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        if (parent == rootRect)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return;
        }

        Vector3[] corners = new Vector3[4];
        rootRect.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = parent.InverseTransformPoint(corners[2]);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = max - min;
        rectTransform.anchoredPosition = (min + max) * 0.5f - parent.rect.center;
    }
}
