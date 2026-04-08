using UnityEngine;
using static BattleRuntimeDefinitions;

public class RuneBuffRuntime
{
    private readonly TrainingBattleManager battleManager;

    public RuneBuffRuntime(TrainingBattleManager battleManager)
    {
        this.battleManager = battleManager;
    }

    public int ApplyCardDamageRuntimeModifiers(Card sourceCard, int damage)
    {
        if (damage <= 0 || sourceCard == null)
        {
            return Mathf.Max(0, damage);
        }

        int finalDamage = damage;
        if (sourceCard.character == Character.Rune && GetPlayerBuffStack(RuneAttackBuffId) > 0)
        {
            finalDamage *= 2;
        }

        return Mathf.Max(0, finalDamage);
    }

    public void ReplayTurnEndTriggeredEffects()
    {
        int runeProtection = GetPlayerBuffStack(RuneProtectionBuffId);
        if (runeProtection > 0 && battleManager.playerData != null)
        {
            battleManager.playerData.AddDefense(runeProtection);
        }
    }

    private int GetPlayerBuffStack(int buffId)
    {
        return battleManager?.playerData != null ? battleManager.playerData.GetBuffStack(buffId) : 0;
    }
}
