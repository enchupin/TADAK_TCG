using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace UI
{
    /// <summary>
    /// 로비 버튼의 호버 효과를 관리하는 컴포넌트
    /// 마우스 호버 시 크기 1.2배 확대 및 불투명도 증가 효과 적용
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LobbyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("호버 효과 설정")]
        [SerializeField] private float hoverScale = 1.2f;
        [SerializeField] private float normalAlpha = 0.8f;
        [SerializeField] private float hoverAlpha = 1.0f;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Vector3 originalScale;
        private CanvasGroup canvasGroup;
        private Coroutine currentAnimation;
        private bool isHovering = false;

        private void Awake()
        {
            originalScale = transform.localScale;
            
            // CanvasGroup이 없으면 추가
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 초기 알파값 설정
            canvasGroup.alpha = normalAlpha;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            isHovering = true;
            currentAnimation = StartCoroutine(AnimateHover(true));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }
            
            isHovering = false;
            currentAnimation = StartCoroutine(AnimateHover(false));
        }

        private IEnumerator AnimateHover(bool hover)
        {
            float startTime = Time.time;
            Vector3 startScale = transform.localScale;
            float startAlpha = canvasGroup.alpha;
            
            Vector3 targetScale = hover ? originalScale * hoverScale : originalScale;
            float targetAlpha = hover ? hoverAlpha : normalAlpha;

            while (Time.time - startTime < animationDuration)
            {
                float elapsed = Time.time - startTime;
                float t = scaleCurve.Evaluate(elapsed / animationDuration);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                yield return null;
            }

            // 최종값 설정
            transform.localScale = targetScale;
            canvasGroup.alpha = targetAlpha;
            
            currentAnimation = null;
        }

        private void OnDisable()
        {
            // 비활성화 시 원래 상태로 복원
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
            
            transform.localScale = originalScale;
            canvasGroup.alpha = normalAlpha;
            isHovering = false;
        }

        // Inspector에서 설정값 변경을 즉시 반영
        private void OnValidate()
        {
            if (Application.isPlaying && !isHovering && canvasGroup != null)
            {
                canvasGroup.alpha = normalAlpha;
            }
        }
    }
}
