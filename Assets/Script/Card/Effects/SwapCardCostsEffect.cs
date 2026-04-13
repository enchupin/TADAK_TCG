using System;
using System.Collections.Generic;

[System.Serializable]
public class SwapCardCostsEffect : ICardEffect
{
    public string subject;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager?.battleContext == null)
        {
            return;
        }

        string resolvedSubject = string.IsNullOrWhiteSpace(subject) ? "DrawnCards" : subject;
        List<Card> cards = battleManager.battleContext.GetContextCards(resolvedSubject);
        if (cards.Count < 2)
        {
            return;
        }

        Card firstCard = cards[0];
        Card secondCard = cards[1];
        if (firstCard == null || secondCard == null)
        {
            return;
        }

        int firstCost = firstCard.cost;
        int secondCost = secondCard.cost;
        firstCard.SetCost(secondCost, false);
        secondCard.SetCost(firstCost, false);
        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, new List<Card> { firstCard, secondCard });
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
