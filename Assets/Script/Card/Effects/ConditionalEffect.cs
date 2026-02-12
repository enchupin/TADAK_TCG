using UnityEngine;
using System.Collections.Generic;

public class ConditionalEffect : ICardEffect
{
    public ConditionData conditionData; // 새로운 조건 데이터
    // Legacy
    public string condition;
    public string conditionValue;
    
    public List<ICardEffect> successEffects = new List<ICardEffect>();
    public List<ICardEffect> failEffects = new List<ICardEffect>();

    public void Execute(TrainingBattleManager battleManager)
    {
        bool isMet = false;
        
        // 1. 신규 데이터 구조 우선 확인
        if (conditionData != null && conditionData.checks != null && conditionData.checks.Count > 0)
        {
            isMet = ConditionEvaluator.Evaluate(conditionData, battleManager);
        }
        // 2. 레거시 데이터 확인 (필요 시)
        else if (!string.IsNullOrEmpty(condition))
        {
            isMet = ConditionEvaluator.Evaluate(condition, conditionValue, battleManager);
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
