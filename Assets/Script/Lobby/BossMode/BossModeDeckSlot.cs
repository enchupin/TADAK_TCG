using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BossModeDeckSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BossModeDeckSelection selection;
    [SerializeField] private int panelIndex;
    [SerializeField] private Image background;
    private Color normalColor;
    private bool initialized;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) selection?.ToggleDeck(panelIndex);
    }

    public void SetSelected(bool selected)
    {
        if (background == null) return;
        if (!initialized) {
            normalColor = background.color;
            initialized = true;
        }
        background.color = selected ? new Color(0.35f, 0.8f, 1f, normalColor.a) : normalColor;
    }
}
