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
    [SerializeField] protected float hoverScale = 1.2f;
    [SerializeField] protected float animationDuration = 0.2f;
    
    protected Vector3 originalScale;
    protected Coroutine currentAnimation;
    public bool isHoverable;
    
    protected virtual void Awake()
    {
        originalScale = transform.localScale;
        isHoverable = true;
    }
    
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHoverable) return;
        StopAnimation();
        currentAnimation = StartCoroutine(AnimateScale(originalScale * hoverScale));
    }
    
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        StopAnimation();
        currentAnimation = StartCoroutine(AnimateScale(originalScale));
    }

    public void SetHoverScale(float scale)
    {
        hoverScale = scale;
    }
    
    public void SetAnimationDuration(float duration)
    {
        animationDuration = duration;
    }

    public void StopAnimation()
    {
        if (currentAnimation != null) 
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    public void ResetHoverState()
    {
        StopAnimation();
        transform.localScale = originalScale;
    }
    
    protected virtual IEnumerator AnimateScale(Vector3 target)
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
