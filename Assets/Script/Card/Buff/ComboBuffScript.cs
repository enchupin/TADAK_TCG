using UnityEngine;

public sealed class ComboBuffScript : PlayerBuffScript
{
    private int damageHitCount;

    public override int BuffId => CreamBuffRuntimeUtility.ComboBuffId;

    public override void ResetForCombat(TrainingBattleManager battleManager, PlayerData player)
    {
        damageHitCount = 0;
    }

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, PlayerData player, int stack, float currentMultiplier)
    {
        if (stack <= 0 || damageHitCount < 6)
        {
            return currentMultiplier;
        }

        return currentMultiplier * 2f;
    }

    public override void OnPlayerDamageDealt(TrainingBattleManager battleManager, PlayerData player, Monster targetMonster, int dealtDamage, int stack)
    {
        if (stack <= 0 || dealtDamage <= 0)
        {
            return;
        }

        if (damageHitCount >= 6)
        {
            damageHitCount = 0;
            return;
        }

        damageHitCount++;
    }
}
