using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 덱 위 카드를 확인하고 그중 한 장을 뽑는 이펙트
/// </summary>
public class ScryEffect : ICardEffect
{
    public int count;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager?.usableDeckManager == null || battleManager.handManager == null)
        {
            return;
        }

        List<Card> drawPile = battleManager.usableDeckManager.GetDrawPile();
        int scryCount = Mathf.Min(count, drawPile.Count);
        if (scryCount <= 0)
        {
            return;
        }

        List<Card> viewedCards = drawPile.GetRange(0, scryCount);
        if (battleManager.OpenSelectCardPanel(viewedCards, 1, selectedCards => ApplySelection(battleManager, selectedCards)))
        {
            return;
        }

        ApplySelection(battleManager, new List<Card> { viewedCards[0] });
    }

    private void ApplySelection(TrainingBattleManager battleManager, List<Card> selectedCards)
    {
        if (battleManager?.usableDeckManager == null || battleManager.handManager == null)
        {
            return;
        }

        Card selectedCard = selectedCards != null && selectedCards.Count > 0 ? selectedCards[0] : null;
        if (selectedCard == null)
        {
            return;
        }

        if (!battleManager.usableDeckManager.RemoveFromDrawPile(selectedCard))
        {
            Debug.LogWarning("[ScryEffect] 선택한 카드를 덱에서 찾지 못했습니다");
            return;
        }

        battleManager.handManager.AddCard(selectedCard);
        battleManager.battleContext?.OnCardsDrawn(1);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
