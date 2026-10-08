using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class ConditionalEffect : ICardEffect
{
    public ConditionData conditionData; // 새로운 조건 데이터
    public List<ICardEffect> successEffects = new List<ICardEffect>();
    public List<ICardEffect> failEffects = new List<ICardEffect>();

    public void Execute(TrainingBattleManager battleManager)
    {
        battleManager?.StartCoroutine(CardEffectSequence.Run(ExecuteSequence(battleManager), battleManager));
    }

    public IEnumerator ExecuteSequence(TrainingBattleManager battleManager, int? amount = null)
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
        
        yield return CardEffectSequence.Execute(effectsToRun, battleManager);

        battleManager.UpdateAllUI();
    }
}
