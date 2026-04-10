using UnityEngine;
using static BattleRuntimeDefinitions;

public sealed class EnhancedCorrosionMonsterBuffScript : MonsterBuffScript
{
    public override int BuffId => EnhancedCorrosionBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetIncomingDamageMultiplier(BuffId);

        return Mathf.Max(currentMultiplier, multiplier);
    }

    public override bool TryConsumeIncomingDamageBuff(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return false;
        }

        monster.ConsumeBuffStack(BuffId, 1);
        return true;
    }
}
