using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 기본카드(카드ID 끝자리 010/020)만 드로우하는 효과
/// </summary>
[System.Serializable]
public class DrawBasicEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

        Character? sourceCharacter = null;
        if (battleManager.battleContext != null) {
            Card lastPlayedCard = battleManager.battleContext.GetLastPlayedCard();
            if (lastPlayedCard != null) {
                sourceCharacter = lastPlayedCard.character;
            }
        }

        List<Card> drawnCards = battleManager.DrawBasicCardsAndGet(finalAmount, sourceCharacter);
        ExecuteOnDrawnCards(battleManager, drawnCards);
    }

    private void ExecuteOnDrawnCards(TrainingBattleManager battleManager, List<Card> drawnCards)
    {
        if (battleManager?.battleContext == null || onActions == null || drawnCards == null || drawnCards.Count == 0) {
            return;
        }

        foreach (Card drawnCard in drawnCards) {
            if (drawnCard == null) {
                continue;
            }

            battleManager.battleContext.SetContextCards("DrawnCard", new List<Card> { drawnCard });

            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, 1);
            }

            battleManager.battleContext.ClearContextCards("DrawnCard");
        }
    }
}
