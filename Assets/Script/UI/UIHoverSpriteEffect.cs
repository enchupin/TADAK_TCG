using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UIHoverSpriteEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("호버 이미지")]
    [Tooltip("마우스를 올렸을 때 표시할 이미지이며 비워두면 기존 이미지를 유지")]
    [SerializeField] private Sprite hoverSprite;

    private Image targetImage;
    private Selectable selectable;
    private Sprite originalSprite;
    private bool isHovered;
    private bool isSpriteChanged;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateHoverSprite();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        RestoreSprite();
    }

    private void LateUpdate()
    {
        // 호버 중 버튼이나 상위 CanvasGroup의 상호작용 상태가 바뀌는 경우도 반영
        if (isHovered)
        {
            UpdateHoverSprite();
        }
    }

    private void UpdateHoverSprite()
    {
        if (hoverSprite == null || (selectable != null && (!selectable.IsActive() || !selectable.IsInteractable())))
        {
            RestoreSprite();
            return;
        }

        if (!isSpriteChanged)
        {
            originalSprite = targetImage.sprite;
            isSpriteChanged = true;
        }

        targetImage.sprite = hoverSprite;
    }

    private void OnDisable()
    {
        isHovered = false;
        RestoreSprite();
    }

    private void RestoreSprite()
    {
        if (isSpriteChanged && targetImage != null)
        {
            targetImage.sprite = originalSprite;
        }

        isSpriteChanged = false;
    }
}
