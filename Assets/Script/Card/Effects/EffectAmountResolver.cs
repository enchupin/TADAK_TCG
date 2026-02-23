public static class EffectAmountResolver
{
    public static int Resolve(int amount, string amountFormula, BattleContext context, PlayerData player = null)
    {
        if (!string.IsNullOrWhiteSpace(amountFormula))
        {
            return FormulaEvaluator.Evaluate(amountFormula, context, player);
        }

        return amount;
    }
}
