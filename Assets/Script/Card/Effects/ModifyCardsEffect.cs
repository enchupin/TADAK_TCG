using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModifyCardsEffect : ICardEffect
{
    public string source;
    public string subject;
    public string param;
    public string operation;
    public int amount;
    public string amountFormula;
    public int effectIndex;
    public string durationText;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null) {
            return;
        }

        List<Card> targetCards = CardEffectRuntimeUtility.ResolveCards(battleManager, source, subject);
        if (targetCards.Count == 0) {
            return;
        }

        ModifyCardEffect singleModify = new ModifyCardEffect
        {
            source = source,
            subject = subject,
            param = param,
            operation = operation,
            amount = amount,
            amountFormula = amountFormula,
            effectIndex = effectIndex,
            durationText = durationText
        };

        foreach (Card card in targetCards) {
            battleManager.battleContext?.SetContextCards("ModifyTarget", new List<Card> { card });
            singleModify.source = "ModifyTarget";
            singleModify.Execute(battleManager, forwardedAmount);
            battleManager.battleContext?.ClearContextCards("ModifyTarget");
        }

        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, targetCards);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }
}
