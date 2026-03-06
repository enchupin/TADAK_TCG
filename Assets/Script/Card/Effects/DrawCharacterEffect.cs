using UnityEngine;

[System.Serializable]
public class DrawCharacterEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        if (battleManager == null)
        {
            return;
        }

        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

        if (finalAmount <= 0)
        {
            return;
        }

        Character? sourceCharacter = null;
        if (battleManager.battleContext != null)
        {
            Card lastPlayedCard = battleManager.battleContext.GetLastPlayedCard();
            if (lastPlayedCard != null)
            {
                sourceCharacter = lastPlayedCard.character;
            }
        }

        if (!sourceCharacter.HasValue)
        {
            Debug.LogWarning("[DrawCharacterEffect] 직업 기준 드로우 실행 실패: 대상 캐릭터를 찾지 못했습니다.");
            return;
        }

        battleManager.DrawCharacterCards(finalAmount, sourceCharacter);
    }
}
