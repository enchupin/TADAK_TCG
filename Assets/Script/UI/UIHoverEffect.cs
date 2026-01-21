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
    [SerializeField] private float animationSpeed = 5f;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    
    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }
    
    void Update()
    {
        // 부드러운 애니메이션
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}
