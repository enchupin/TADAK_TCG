using UnityEngine;
using System.Collections.Generic;

public class ConditionalEffect : ICardEffect
{
    public ConditionData conditionData; // 새로운 조건 데이터
    public List<ICardEffect> successEffects = new List<ICardEffect>();
    public List<ICardEffect> failEffects = new List<ICardEffect>();

    public void Execute(TrainingBattleManager battleManager)
    {
        bool isMet;
        if (conditionData != null && conditionData.checks != null && conditionData.checks.Count > 0) {
            isMet = ConditionEvaluator.Evaluate(conditionData, battleManager);
        }
        else
        {
            // 조건이 없으면 성공으로 간주?
            isMet = true;
        }
        
        List<ICardEffect> effectsToRun = isMet ? successEffects : failEffects;
        
        if (effectsToRun != null)
        {
            foreach (var effect in effectsToRun)
            {
                effect.Execute(battleManager);
            }
        }
        
        battleManager.UpdateAllUI();
    }
}
