using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 손패 선택 중 후보 카드에 잠시 붙는 클릭 수신기 (선택이 끝나면 제거)
/// </summary>
public sealed class HandCardSelectionClick : MonoBehaviour, IPointerClickHandler
{
    public CardController Controller { get; private set; }
    private Action<CardController> onClick;

    public void Bind(CardController controller, Action<CardController> clickHandler)
    {
        Controller = controller;
        onClick = clickHandler;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        onClick?.Invoke(Controller);
    }
}
