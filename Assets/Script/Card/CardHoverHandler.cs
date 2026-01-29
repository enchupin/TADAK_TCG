using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 카드 호버링 효과를 담당하는 핸들러
/// 마우스 올렸을 때 확대, 내렸을 때 축소
/// </summary>
public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.4f; // 호버링 시 확대 크기
    [SerializeField] private float hoverDuration = 0.15f; // 호버링 애니메이션 시간 (초)
    
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isDragging = false;
    
    private void Awake()
    {
        originalScale = transform.localScale;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging)
        {
            StopCurrentAnimation();
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale * hoverScale));
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging)
        {
            StopCurrentAnimation();
            scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
        }
    }
    
    /// <summary>
    /// 드래그 시작 시 호출 - 호버 상태라면 확대된 크기 유지
    /// </summary>
    public void OnDragStart()
    {
        isDragging = true;
        // 현재 스케일 유지 (호버링되어 있다면 확대 상태 유지)
    }
    
    /// <summary>
    /// 드래그 종료 시 호출
    /// </summary>
    public void OnDragEnd()
    {
        isDragging = false;
        StopCurrentAnimation();
        scaleCoroutine = StartCoroutine(ScaleAnimation(originalScale));
    }
    
    /// <summary>
    /// 즉시 원래 크기로 복원
    /// </summary>
    public void ResetScaleImmediate()
    {
        StopCurrentAnimation();
        transform.localScale = originalScale;
    }
    
    private void StopCurrentAnimation()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }
    
    private IEnumerator ScaleAnimation(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hoverDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    public Vector3 OriginalScale => originalScale;
}
