using UnityEngine;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class WeakBuffScript : MonsterBuffScript
{
    public override int BuffId => WeakBuffId;

    public override float GetOutgoingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetOutgoingDamageMultiplier(BuffId);
        return currentMultiplier * multiplier;
    }

    public override void OnMonsterTurnEnd(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
}
