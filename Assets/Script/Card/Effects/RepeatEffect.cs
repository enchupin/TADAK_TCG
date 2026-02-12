using UnityEngine;
using System.Collections.Generic;

public class RepeatEffect : ICardEffect
{
    public int count; // Fixed repetition count
    public string countFormula; // Formula for repetition count
    public List<ICardEffect> effectsToRepeat = new List<ICardEffect>();

    public void Execute(TrainingBattleManager battleManager)
    {
        int repetitions = count;
        if (!string.IsNullOrEmpty(countFormula))
        {
            repetitions = FormulaEvaluator.Evaluate(countFormula, battleManager.battleContext, battleManager.playerData);
        }

        for (int i = 0; i < repetitions; i++)
        {
            foreach (var effect in effectsToRepeat)
            {
                effect.Execute(battleManager);
            }
        }
    }
}
