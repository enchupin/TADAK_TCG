using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DrawCharacterEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

        if (finalAmount <= 0)
        {
            return;
        }

        Character? sourceCharacter = null;
        if (battleManager.battleContext != null)
        {
            Card lastPlayedCard = battleManager.battleContext.GetLastPlayedCard();
            if (lastPlayedCard != null)
            {
                sourceCharacter = lastPlayedCard.character;
            }
        }

        if (!sourceCharacter.HasValue)
        {
            Debug.LogWarning("[DrawCharacterEffect] 직업 기준 드로우 실행 실패: 대상 캐릭터를 찾지 못했습니다.");
            return;
        }

        List<Card> drawnCards = battleManager.DrawCharacterCardsAndGet(finalAmount, sourceCharacter);
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
