using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayExhaustedCardsEffect : ICardEffect
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

        List<Card> exhaustPile = battleManager.usableDeckManager.GetExhaustPile();
        if (exhaustPile == null || exhaustPile.Count == 0)
        {
            yield break;
        }

        int replayCount = ResolveReplayCount(battleManager, exhaustPile.Count, forwardedAmount);
        if (replayCount <= 0)
        {
            yield break;
        }

        List<Card> candidates = new List<Card>(exhaustPile);
        Monster currentTarget = battleManager.currentTarget;
        for (int i = 0; i < replayCount && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            Card replayCard = candidates[index]?.CloneForRuntimeCopy();
            candidates.RemoveAt(index);

            if (replayCard == null)
            {
                continue;
            }

            yield return TriggeredCardExecutionUtility.ExecuteTriggeredCardSequence(
                battleManager,
                replayCard,
                currentTarget,
                resolveDestination: false,
                triggerPowerEffects: false,
                allowRepeats: false,
                applyPostPlayKeywords: false);
        }

        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private int ResolveReplayCount(TrainingBattleManager battleManager, int maxCount, int forwardedAmount)
    {
        int resolvedAmount = amount;
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            resolvedAmount = FormulaEvaluator.Evaluate(amountFormula, battleManager?.battleContext, battleManager?.playerData, forwardedAmount);
        }
        else if (resolvedAmount <= 0)
        {
            resolvedAmount = forwardedAmount > 0 ? forwardedAmount : 1;
        }

        return Mathf.Clamp(resolvedAmount, 0, maxCount);
    }
}
