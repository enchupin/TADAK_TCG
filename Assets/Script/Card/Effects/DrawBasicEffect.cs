using UnityEngine;

/// <summary>
/// 기본카드(카드ID 끝자리 010/020)만 드로우하는 효과
/// </summary>
[System.Serializable]
public class DrawBasicEffect : ICardEffect
{
    public int amount;
    public string amountFormula;

    public void Execute(TrainingBattleManager battleManager)
    {
        int finalAmount = string.IsNullOrWhiteSpace(amountFormula)
            ? amount : FormulaEvaluator.Evaluate(amountFormula, battleManager.battleContext, battleManager.playerData);

        Character? sourceCharacter = null;
        if (battleManager.battleContext != null) {
            Card lastPlayedCard = battleManager.battleContext.GetLastPlayedCard();
            if (lastPlayedCard != null) {
                sourceCharacter = lastPlayedCard.character;
            }
        }

        battleManager.DrawBasicCards(finalAmount, sourceCharacter);
    }
}
