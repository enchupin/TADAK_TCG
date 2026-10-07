using System.Collections;
using UnityEngine;

public class UseTopDeckCardsEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager, forwardedAmount), battleManager));
    }

    public IEnumerator ExecuteSequence(TrainingBattleManager battleManager, int? amount = null)
    {
        int forwardedAmount = amount ?? 0;
        if (battleManager?.usableDeckManager == null)
        {
            yield break;
        }

        int useCount = ResolveUseCount(battleManager, forwardedAmount);
        if (useCount <= 0)
        {
            yield break;
        }

        Monster currentTarget = battleManager.currentTarget;
        for (int i = 0; i < useCount; i++)
        {
            Card topDeckCard = battleManager.usableDeckManager.DrawCard();
            if (topDeckCard == null)
            {
                break;
            }

            yield return TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
                battleManager,
                topDeckCard,
                currentTarget,
                resolveDestination: true,
                triggerPowerEffects: true,
                allowRepeats: true,
                applyPostPlayKeywords: true);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private int ResolveUseCount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(amountFormula, battleManager?.battleContext, battleManager?.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return forwardedAmount > 0 ? forwardedAmount : 1;
    }
}
