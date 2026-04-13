using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 고급 UI 호버 효과
/// 크기, 색상, 사운드 등 다양한 효과 지원
/// </summary>
public class UIHoverEffectAdvanced : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("스케일 효과")]
    [SerializeField] private bool enableScale = true;
    [SerializeField] private float hoverScale = 1.2f;
    
    [Header("색상 효과")]
    [SerializeField] private bool enableColorChange = false;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    
    [Header("애니메이션")]
    [SerializeField] private float animationSpeed = 5f;
    
    [Header("사운드 (선택)")]
    [SerializeField] private AudioClip hoverSound;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Image image;
    private Color targetColor;
    private AudioSource audioSource;
    
    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        image = GetComponent<Image>();
        if (image != null)
        {
            normalColor = image.color;
            targetColor = normalColor;
        }
        
        // AudioSource 추가 (사운드 사용 시)
        if (hoverSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    void Update()
    {
        // 스케일 애니메이션
        if (enableScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
        
        // 색상 애니메이션
        if (enableColorChange && image != null)
        {
            image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * animationSpeed);
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enableScale)
            targetScale = originalScale * hoverScale;
        
        if (enableColorChange)
            targetColor = hoverColor;
        
        // 사운드 재생
        if (hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (enableScale)
            targetScale = originalScale;
        
        if (enableColorChange)
            targetColor = normalColor;
    }

    public void ResetHoverState()
    {
        if (enableScale)
        {
            targetScale = originalScale;
            transform.localScale = originalScale;
        }

        if (enableColorChange)
        {
            targetColor = normalColor;
            if (image != null)
            {
                image.color = normalColor;
            }
        }
    }
}
