using UnityEngine;
using UnityEngine.UI;

public class TargetingArrow : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image lineImage;

    public void Initialize()
    {
        // Image를 추가하면 자동으로 RectTransform이 추가
        lineImage = gameObject.AddComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        lineImage.color = new Color(1f, 0f, 0f, 0.6f);
        lineImage.raycastTarget = false; // 화살표가 몬스터를 가려 마우스 클릭을 방해하는 것을 방지
        
        // Pivot 중단 좌측 (시작점)으로 설정
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        
        gameObject.SetActive(false);
    }

    public void UpdateArrow(Vector2 startPos, Vector2 endPos)
    {
        if (rectTransform == null) return;

        rectTransform.position = startPos;
        
        Vector2 direction = endPos - startPos;
        float distance = direction.magnitude;
        
        // 크기 설정
        rectTransform.sizeDelta = new Vector2(distance, 15f); // 두께 15f
        
        // 회전 설정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
