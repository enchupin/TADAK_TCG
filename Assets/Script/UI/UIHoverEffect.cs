using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 모든 UI에 붙일 수 있는 호버 효과 컴포넌트
/// 마우스 올리면 확대, 내리면 원래 크기
/// </summary>
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("호버 설정")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float animationDuration = 0.2f;
    
    private Vector3 originalScale;
    private Coroutine currentAnimation;
    
    void Awake()
    {
        originalScale = transform.localScale;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateScale(originalScale * hoverScale));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateScale(originalScale));
    }

    private IEnumerator AnimateScale(Vector3 target)
    {
        Vector3 start = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, elapsedTime / animationDuration);
            yield return null;
        }

        transform.localScale = target;
        currentAnimation = null;
    }
}
