using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class StrengthBuffScript : PlayerBuffScript
{
    public override int BuffId => StrengthBuffId;

    public override int GetCalculatedCardDamageBonus(TrainingBattleManager battleManager, PlayerData player, float strengthMultiplier, int stack, int currentBonus)
    {
        if (stack == 0 || strengthMultiplier <= 0f)
        {
            return currentBonus;
        }

        int bonus = Mathf.FloorToInt(stack * strengthMultiplier);
        return currentBonus + bonus;
    }
}
