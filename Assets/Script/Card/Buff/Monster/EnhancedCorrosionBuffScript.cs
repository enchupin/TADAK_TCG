using UnityEngine;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class EnhancedCorrosionBuffScript : MonsterBuffScript
{
    public override int BuffId => EnhancedCorrosionBuffId;

    public override float GetIncomingDamageMultiplier(TrainingBattleManager battleManager, Monster monster, int stack, float currentMultiplier)
    {
        if (stack <= 0)
        {
            return currentMultiplier;
        }

        float multiplier = BuffValueUtility.GetIncomingDamageMultiplier(BuffId);

        return currentMultiplier * multiplier;
    }

    // 관통도 부식과 같은 규칙으로 몬스터 턴 시작 시 1 감소 (R-85)
    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
}
