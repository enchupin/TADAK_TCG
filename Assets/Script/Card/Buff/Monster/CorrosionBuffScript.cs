using UnityEngine;
using static BattleRuntimeDefinitions;

namespace MonsterBuffs
{
public sealed class CorrosionBuffScript : MonsterBuffScript
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

        return currentMultiplier * multiplier;
    }

    // 보유하는 동안 모든 피해를 증폭하고 몬스터 턴 시작 시 1 감소 (R-85)
    public override void OnMonsterTurnStart(TrainingBattleManager battleManager, Monster monster, int stack)
    {
        if (monster == null || stack <= 0 || monster.GetBuffStack(EnhancedCorrosionBuffId) > 0)
        {
            return;
        }

        monster.ConsumeBuffStack(BuffId, 1);
    }
}
}
