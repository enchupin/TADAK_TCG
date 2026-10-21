using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DrawCharacterEffect : ICardEffect
{
    public int amount;
    public string amountFormula;
    public List<ICardEffect> onActions;
    // 없음: 같은 캐릭터 카드, Other: 다른 아군 카드, DealsDamage: 피해를 입히는 카드
    public string operation;

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

        List<Card> drawnCards;
        Character source = sourceCharacter.Value;
        if (string.Equals(operation, "Other", System.StringComparison.OrdinalIgnoreCase)) {
            drawnCards = battleManager.DrawMatchingCardsAndGet(finalAmount, card => card.character != source && card.character != Character.Monster);
        }
        else if (string.Equals(operation, "DealsDamage", System.StringComparison.OrdinalIgnoreCase)) {
            drawnCards = battleManager.DrawMatchingCardsAndGet(finalAmount, DealsDamage);
        }
        else {
            drawnCards = battleManager.DrawCharacterCardsAndGet(finalAmount, sourceCharacter);
        }

        ExecuteOnDrawnCards(battleManager, drawnCards);
    }

    // 피해(공격·직접 피해·체력 감소)를 주는 효과가 있는 카드
    private static bool DealsDamage(Card card)
    {
        if (card?.effects == null) {
            return false;
        }

        return card.effects.Exists(effect => effect is AttackEffect || effect is DamageEffect || effect is DamageByBuffStackEffect
            || (effect is RepeatEffect repeat && (repeat.effectToRepeat is AttackEffect || repeat.effectToRepeat is DamageEffect)));
    }

    private void ExecuteOnDrawnCards(TrainingBattleManager battleManager, List<Card> drawnCards)
    {
        if (battleManager?.battleContext == null || onActions == null || drawnCards == null || drawnCards.Count == 0) {
            return;
        }

        foreach (Card drawnCard in drawnCards) {
            if (drawnCard == null) {
                continue;
            }

            battleManager.battleContext.SetContextCards("DrawnCard", new List<Card> { drawnCard });

            foreach (ICardEffect onAction in onActions) {
                onAction?.Execute(battleManager, 1);
            }

            battleManager.battleContext.ClearContextCards("DrawnCard");
        }
    }
}
