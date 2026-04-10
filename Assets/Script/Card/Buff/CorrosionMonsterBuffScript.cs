using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class CorrosionMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => CorrosionBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        if (monster == null || stack <= 0 || monster.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return currentMultiplier;
        }

        bool playerEnhancesCorrosion = battleManager?.playerData != null
            && battleManager.playerData.GetBuffStack(CorrosionEnhanceBuffId) > 0;
        float multiplier = playerEnhancesCorrosion
            ? BuffValueUtility.GetIncomingDamageMultiplier(EnhancedCorrosionBuffId)
            : BuffValueUtility.GetIncomingDamageMultiplier(BuffId);

        return Mathf.Max(currentMultiplier, multiplier);
    }

    public override bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0 || monster.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return false;
        }

        monster.ConsumeBuffStack(BuffId, 1);
        return true;
    }
}
