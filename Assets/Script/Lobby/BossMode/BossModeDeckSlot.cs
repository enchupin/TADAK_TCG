using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보스 모드 덱 선택 팝업의 저장 덱 칸 (클릭하면 그 덱으로 선택 완료)
public class BossModeDeckSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BossModeDeckSelection selection;
    [SerializeField] private int panelIndex;
    [SerializeField] private Image background;
    private Color normalColor;
    private bool initialized;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) selection?.ChooseDeck(panelIndex);
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
