using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModifyCardEffect : ICardEffect
{
    public string source;
    public string subject;
    public string param;
    public string operation;
    public int amount;
    public string amountFormula;
    public int effectIndex;
    public string durationText;

    public void Execute(TrainingBattleManager battleManager)
    {
        Execute(battleManager, 0);
    }

    public void Execute(TrainingBattleManager battleManager, int forwardedAmount)
    {
        if (battleManager == null) {
            return;
        }

        List<Card> targetCards = CardEffectRuntimeUtility.ResolveCards(battleManager, source, subject);
        if (targetCards.Count == 0) {
            return;
        }

        int resolvedAmount = CardEffectRuntimeUtility.ResolveCardValueAmount(battleManager, amount, amountFormula, forwardedAmount);
        foreach (Card card in targetCards) {
            ApplyToCard(card, resolvedAmount);
        }

        CardEffectRuntimeUtility.RefreshCardDisplays(battleManager, targetCards);
        battleManager.RefreshHandPlayableState();
        battleManager.UpdateAllUI();
    }

    private void ApplyToCard(Card card, int resolvedAmount)
    {
        if (card == null || string.IsNullOrWhiteSpace(param)) {
            return;
        }

        switch (param)
        {
            case "Cost":
                ApplyCostChange(card, resolvedAmount);
                return;
            case "Damage":
                ApplyDamageChange(card, resolvedAmount);
                return;
            case "EffectAmount":
                ApplyEffectAmountChange(card, resolvedAmount);
                return;
            default:
                Debug.LogWarning($"[ModifyCardEffect] 지원하지 않는 param입니다: {param}");
                return;
        }
    }

    private void ApplyCostChange(Card card, int resolvedAmount)
    {
        bool turnOnly = string.Equals(durationText, "Turn", System.StringComparison.OrdinalIgnoreCase);
        if (string.Equals(operation, "Set", System.StringComparison.OrdinalIgnoreCase)) {
            card.SetCost(resolvedAmount, turnOnly);
            return;
        }

        card.ApplyCostModifier(resolvedAmount, turnOnly);
    }

    private void ApplyDamageChange(Card card, int resolvedAmount)
    {
        foreach (ICardEffect effect in card.effects)
        {
            if (effect is AttackEffect attack)
            {
                attack.amount = ApplyOperation(attack.amount, resolvedAmount);
                return;
            }

            if (effect is DamageEffect damage)
            {
                damage.amount = ApplyOperation(damage.amount, resolvedAmount);
                return;
            }
        }
    }

    private void ApplyEffectAmountChange(Card card, int resolvedAmount)
    {
        if (card.effects == null || effectIndex < 0 || effectIndex >= card.effects.Count) {
            return;
        }

        ICardEffect targetEffect = card.effects[effectIndex];
        switch (targetEffect)
        {
            case AttackEffect attack:
                attack.amount = ApplyOperation(attack.amount, resolvedAmount);
                break;
            case DamageEffect damage:
                damage.amount = ApplyOperation(damage.amount, resolvedAmount);
                break;
            case BarrierEffect barrier:
                barrier.amount = ApplyOperation(barrier.amount, resolvedAmount);
                break;
            case HealEffect heal:
                heal.amount = ApplyOperation(heal.amount, resolvedAmount);
                break;
            case BuffEffect buff:
                buff.amount = ApplyOperation(buff.amount, resolvedAmount);
                break;
            case DrawEffect draw:
                draw.amount = ApplyOperation(draw.amount, resolvedAmount);
                break;
            case DrawBasicEffect drawBasic:
                drawBasic.amount = ApplyOperation(drawBasic.amount, resolvedAmount);
                break;
            case DrawCharacterEffect drawCharacter:
                drawCharacter.amount = ApplyOperation(drawCharacter.amount, resolvedAmount);
                break;
            case HpLossEffect hpLoss:
                hpLoss.amount = ApplyOperation(hpLoss.amount, resolvedAmount);
                break;
            default:
                Debug.LogWarning($"[ModifyCardEffect] EffectAmount 변경을 지원하지 않는 효과입니다: {targetEffect.GetType().Name}");
                break;
        }
    }

    private int ApplyOperation(int currentValue, int resolvedAmount)
    {
        if (string.Equals(operation, "Set", System.StringComparison.OrdinalIgnoreCase)) {
            return Mathf.Max(0, resolvedAmount);
        }

        return Mathf.Max(0, currentValue + resolvedAmount);
    }
}
