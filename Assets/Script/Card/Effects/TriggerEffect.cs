using UnityEngine;

public class TriggerEffect : ICardEffect
{
    public string timing;
    public TargetType target = TargetType.None;
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 1);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null || string.IsNullOrWhiteSpace(timing))
        {
            return;
        }

        int repeatCount = ResolveAmount(battleManager, forwardedAmount);
        if (string.Equals(timing, "OnTurnEnd", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.AddTurnEndTriggerRepeat(repeatCount);
            return;
        }

        if (string.Equals(timing, "Feather", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.TriggerFeather(target, repeatCount);
            return;
        }

        if (string.Equals(timing, "FeatherUntilEmpty", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.TriggerFeatherUntilEmpty(target);
            return;
        }

        if (string.Equals(timing, "ReplayExhaustedFeather", System.StringComparison.OrdinalIgnoreCase))
        {
            battleManager.ReplayExhaustedFeathers();
            return;
        }

        Debug.LogWarning($"[TriggerEffect] 아직 지원하지 않는 timing입니다: {timing}");
    }

    private int ResolveAmount(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return Mathf.Max(1, FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData, forwardedAmount));
        }

        if (amount > 0)
        {
            return amount;
        }

        return forwardedAmount > 0 ? forwardedAmount : 1;
    }
}
