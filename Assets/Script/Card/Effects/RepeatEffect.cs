using System.Collections;
using UnityEngine;

public class RepeatEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public ICardEffect effectToRepeat;

    public void Execute(TrainingBattleManager battleManager)
    {
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager), battleManager));
    }

    public void Execute(TrainingBattleManager battleManager, int amount)
    {
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager, amount), battleManager));
    }

    public IEnumerator ExecuteSequence(TrainingBattleManager battleManager, int? amount = null)
    {
        if (battleManager == null || effectToRepeat == null) {
            yield break;
        }

        int forwardedAmount = amount ?? 0;
        int repeatCount = ResolveRepeatCount(battleManager, forwardedAmount);
        for (int i = 0; i < repeatCount; i++) {
            yield return effectToRepeat.ExecuteSequence(battleManager, forwardedAmount);
        }
    }

    private int ResolveRepeatCount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula)) {
            return Mathf.Max(0, FormulaEvaluator.Evaluate(
                amountFormula,
                battleManager.battleContext,
                battleManager.playerData,
                forwardedAmount));
        }

        if (amount > 0) {
            return amount;
        }

        return Mathf.Max(0, forwardedAmount);
    }
}
